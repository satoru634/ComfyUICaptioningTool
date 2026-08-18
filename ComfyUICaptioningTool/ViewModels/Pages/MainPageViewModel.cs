using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using ComfyUICaptioningTool.Helpers;
using ComfyUICaptioningTool.Models;
using ComfyUICaptioningTool.Services;
using ComfyUILibs.Common;
using ComfyUILibs.Exceptions;
using ComfyUILibs.Models;
using ComfyUILibs.Services;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace ComfyUICaptioningTool.ViewModels.Pages
{
    /// <summary>
    /// ディレクトリ一括タグ付け実行ページ（MainPage）の ViewModel。
    /// 対象ディレクトリ・オプション・prepend/exclude タグを受け取り、
    /// <see cref="ICaptioningService"/> 経由でバッチ処理を実行する。
    /// </summary>
    public partial class MainPageViewModel : ObservableObject, INavigationAware
    {
        /// <summary>
        /// captioning_config.json をベースにした実行結果設定 JSON の出力ファイル名（対象ディレクトリ直下）。
        /// </summary>
        private const string ConfigResultFileName = "captioning_config_result.json";

        /// <summary>captioning_config.json 読み込み時のオプション。プロパティ名の大文字/小文字を区別しない。</summary>
        private static readonly JsonSerializerOptions ConfigReadOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        /// <summary>
        /// 実行結果設定 JSON の書き込みオプション。null のプロパティ（default_workflow/workflows 等）を
        /// 出力しないことで、captioning_config.json と同じフォーマットを維持する（ConfigViewModel と同様）。
        /// </summary>
        private static readonly JsonSerializerOptions ConfigWriteOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        /// <summary>アプリケーション設定。</summary>
        public Setting<AppConfig> Config { get; }

        /// <summary>スナックバー通知サービス。</summary>
        private readonly ISnackbarService _snackbarService;

        /// <summary>
        /// <see cref="ITaggerRunner"/> と prepend/exclude タグから <see cref="ICaptioningService"/> を生成するファクトリー。
        /// 既定はネットワーク通信を伴う実装。テスト時はフェイクに差し替える。
        /// </summary>
        private readonly Func<ITaggerRunner, IReadOnlyList<string>, IReadOnlyList<string>, ICaptioningService> _captioningServiceFactory;

        /// <summary>
        /// Config.Data.ConfigPath と Config.Data.TaggerBackend から読み込んだ ITaggerRunner
        /// （Wd14TaggerRunner または WdV3TimmTaggerRunner）。読み込み失敗時は null。
        /// </summary>
        private ITaggerRunner? _taggerRunner;

        /// <summary>ConfigPath の読み込みに成功し、実行可能な状態かどうか。</summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RunCommand))]
        private bool _isConfigLoaded;

        /// <summary>選択中の対象ディレクトリ。</summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RunCommand))]
        private string? _targetDirectory;

        /// <summary>サブディレクトリも再帰的に処理するか。</summary>
        [ObservableProperty]
        private bool _recursive;

        /// <summary>既存の .txt を上書きするか。</summary>
        [ObservableProperty]
        private bool _overwrite;

        /// <summary>完了後にタグ集計レポート（tags_report.txt）を生成するか。</summary>
        [ObservableProperty]
        private bool _generateReport;

        /// <summary>先頭に追加するタグ（カンマ区切り）。</summary>
        [ObservableProperty]
        private string _prependTagsText = "";

        /// <summary>除外するタグ（カンマ区切り）。</summary>
        [ObservableProperty]
        private string _excludeTagsText = "";

        /// <summary>バッチ処理を実行中かどうか。</summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RunCommand))]
        private bool _isRunning;

        /// <summary>現在処理中のファイルの通し番号（1 始まり）。未実行時は 0。</summary>
        [ObservableProperty]
        private int _progressCurrent;

        /// <summary>処理対象ファイルの総数。未実行時は 0。</summary>
        [ObservableProperty]
        private int _progressTotal;

        /// <summary>
        /// 進捗バーを表示すべきか（1 件以上の処理が開始されたか）。
        /// ProgressTotal が 0 のまま ProgressBar を表示すると Maximum=0 により満杯表示になってしまうため。
        /// </summary>
        public bool HasProgress => ProgressTotal > 0;

        partial void OnProgressTotalChanged(int value) => OnPropertyChanged(nameof(HasProgress));

        /// <summary>直近の処理結果を表す 1 行（例: "[3/42] photo.jpg → OK"）。</summary>
        [ObservableProperty]
        private string _progressText = "";

        /// <summary>完了サマリ（例: "完了: 処理 40, スキップ 1, エラー 1"）。未実行時は空文字。</summary>
        [ObservableProperty]
        private string _summaryText = "";

        /// <summary>1 ファイルごとの処理結果ログ。</summary>
        public ObservableCollection<string> LogEntries { get; } = new();

        /// <summary>
        /// DI コンテナから設定・スナックバーサービスを受け取って初期化する。
        /// <paramref name="captioningServiceFactory"/> はテスト用の差し替え口（省略時は実ネットワーク通信を行う既定実装）。
        /// </summary>
        public MainPageViewModel(
            Setting<AppConfig> config,
            ISnackbarService snackbarService,
            Func<ITaggerRunner, IReadOnlyList<string>, IReadOnlyList<string>, ICaptioningService>? captioningServiceFactory = null)
        {
            Config = config;
            _snackbarService = snackbarService;
            _captioningServiceFactory = captioningServiceFactory
                ?? ((runner, prepend, exclude) => new CaptioningServiceAdapter(runner, prepend, exclude));
        }

        // ── INavigationAware ─────────────────────────────────────────────────

        /// <summary>ページへ遷移するたびに captioning_config.json を再読み込みし、Runner を初期化する。</summary>
        public Task OnNavigatedToAsync() => TryLoadRunnerAsync();

        /// <summary>ページから離れるときは何もしない。</summary>
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        /// <summary>
        /// 設定ページで指定された ConfigPath・TaggerBackend から ITaggerRunner
        /// （Wd14TaggerRunner または WdV3TimmTaggerRunner）を初期化する。
        /// 既存の Runner が <see cref="IAsyncDisposable"/>（WdV3TimmTaggerRunner）の場合、
        /// 常駐プロセスを起動したままリークしないよう再構築前に破棄する。
        /// </summary>
        /// <param name="showErrorSnackbar">
        /// 失敗時にスナックバーでエラーメッセージを表示するか。ページ遷移時（<see cref="OnNavigatedToAsync"/>）は
        /// true、<see cref="RunAsync"/> 完了後の内部的な再構築時は false（今回の実行結果の通知と重複させないため）。
        /// </param>
        private async Task TryLoadRunnerAsync(bool showErrorSnackbar = true)
        {
            if (_taggerRunner is IAsyncDisposable disposableRunner)
                await disposableRunner.DisposeAsync();

            var path = Config.Data.ConfigPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                _taggerRunner = null;
                IsConfigLoaded = false;

                if (showErrorSnackbar)
                {
                    _snackbarService.Show(
                        LocalizationManager.Instance["Common_Error"],
                        LocalizationManager.Instance["Common_ConfigPathNotSet"],
                        ControlAppearance.Danger,
                        new SymbolIcon(SymbolRegular.ErrorCircle24),
                        TimeSpan.FromSeconds(3.0));
                }
                return;
            }

            try
            {
                _taggerRunner = TaggerRunnerFactory.Create(path, Config.Data.TaggerBackend);
                IsConfigLoaded = true;
            }
            catch (ComfyUIException ex)
            {
                _taggerRunner = null;
                IsConfigLoaded = false;

                if (showErrorSnackbar)
                {
                    _snackbarService.Show(
                        LocalizationManager.Instance["Common_Error"],
                        string.Format(LocalizationManager.Instance["Main_ConfigLoadErrorFormat"], ex.Message),
                        ControlAppearance.Danger,
                        new SymbolIcon(SymbolRegular.ErrorCircle24),
                        TimeSpan.FromSeconds(3.0));
                }
            }
        }

        // ── ディレクトリ選択 ───────────────────────────────────────────────────

        /// <summary>フォルダー選択ダイアログを開いて対象ディレクトリを選択する。</summary>
        [RelayCommand]
        private void BrowseDirectory()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = LocalizationManager.Instance["Main_DirectoryDialogTitle"],
            };

            if (!string.IsNullOrWhiteSpace(TargetDirectory))
                dialog.InitialDirectory = TargetDirectory;

            if (dialog.ShowDialog() == true)
                TargetDirectory = dialog.FolderName;
        }

        // ── 別の captioning_config.json からのタグインポート ───────────────────

        /// <summary>ファイル選択ダイアログを開いて、別の captioning_config.json から prepend/exclude タグをインポートする。</summary>
        [RelayCommand]
        private void ImportTagsFromConfig()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = LocalizationManager.Instance["Main_ImportConfigDialogTitle"],
                Filter = "JSON (*.json)|*.json|All files (*.*)|*.*",
            };

            if (dialog.ShowDialog() == true)
                ImportTagsFromFile(dialog.FileName);
        }

        /// <summary>
        /// <paramref name="path"/> が指す captioning_config.json から prepend_tags/exclude_tags を読み込み、
        /// 現在の入力欄（PrependTagsText/ExcludeTagsText）へ追記（大文字小文字無視で重複排除しつつマージ）する。
        /// JSON 構文が不正な場合はエラー表示のみ行い入力欄は変更しない（簡易的なバリデーション）。
        /// ファイルダイアログの操作を伴わずテストできるよう <see cref="ImportTagsFromConfigCommand"/> から分離した公開メソッド。
        /// </summary>
        public void ImportTagsFromFile(string path)
        {
            string json;
            try
            {
                json = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ShowImportError(string.Format(LocalizationManager.Instance["Main_ImportConfigReadErrorFormat"], ex.Message));
                return;
            }

            WorkflowConfig config;
            try
            {
                config = JsonSerializer.Deserialize<WorkflowConfig>(json, ConfigReadOptions) ?? new WorkflowConfig();
            }
            catch (JsonException ex)
            {
                ShowImportError(string.Format(LocalizationManager.Instance["Main_ImportConfigParseErrorFormat"], ex.Message));
                return;
            }

            var importedPrepend = config.PrependTags ?? new List<string>();
            var importedExclude = config.ExcludeTags ?? new List<string>();

            PrependTagsText = string.Join(", ", MergeTagLists(SplitTags(PrependTagsText), importedPrepend));
            ExcludeTagsText = string.Join(", ", MergeTagLists(SplitTags(ExcludeTagsText), importedExclude));

            _snackbarService.Show(
                LocalizationManager.Instance["Common_Completed"],
                string.Format(LocalizationManager.Instance["Main_ImportConfigSuccessFormat"], path),
                ControlAppearance.Success,
                new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                TimeSpan.FromSeconds(4.0));
        }

        private void ShowImportError(string message) => _snackbarService.Show(
            LocalizationManager.Instance["Common_Error"],
            message,
            ControlAppearance.Danger,
            new SymbolIcon(SymbolRegular.ErrorCircle24),
            TimeSpan.FromSeconds(5.0));

        // ── バッチ実行 ────────────────────────────────────────────────────────

        private bool CanRun() => IsConfigLoaded && !IsRunning && !string.IsNullOrWhiteSpace(TargetDirectory);

        /// <summary>対象ディレクトリ内の画像を一括タグ付けし、必要に応じてタグ集計レポートを生成する。</summary>
        [RelayCommand(CanExecute = nameof(CanRun))]
        private async Task RunAsync()
        {
            var directory = TargetDirectory!;

            IsRunning = true;
            LogEntries.Clear();
            SummaryText = "";
            ProgressCurrent = 0;
            ProgressTotal = 0;
            ProgressText = "";

            // Results フォルダへの結果ログ出力（SaveResultLogAsync）は成功・失敗どちらの場合も
            // finally で行うため、finally から参照する値をここで宣言しておく。
            var status = "error";
            var processed = 0;
            var skipped = 0;
            var errors = 0;
            string? errorMessage = null;
            var prependTags = new List<string>();
            var excludeTags = new List<string>();
            WorkflowConfig? usedConfig = null;

            try
            {
                prependTags = MergeTags(_taggerRunner!.PrependTags, PrependTagsText);
                excludeTags = MergeTags(_taggerRunner!.ExcludeTags, ExcludeTagsText);
                usedConfig = await LoadConfigWithMergedTagsAsync(prependTags, excludeTags);

                var service = _captioningServiceFactory(_taggerRunner!, prependTags, excludeTags);

                // System.Progress<T> はコールバックを SynchronizationContext.Post 経由で非同期に配送するため、
                // await 直後に LogEntries/ProgressText の反映を検証するテストが不安定になる。
                // 本 ViewModel の await はすべて UI スレッドのコンテキストを捕捉して再開するため、
                // 同期的に呼び出しても問題ない（かつテストからも決定的に検証できる）。
                var progress = new SynchronousProgress<CaptioningProgress>(OnProgress);
                (processed, skipped, errors) = await service.ProcessDirectoryAsync(
                    directory, Recursive, Overwrite, progress);

                status = "success";
                SummaryText = string.Format(
                    LocalizationManager.Instance["Main_SummaryFormat"], processed, skipped, errors);

                await SaveExecutedConfigAsync(directory, usedConfig);

                if (GenerateReport)
                {
                    await service.GenerateReportAsync(directory, Recursive);
                    LogEntries.Add(string.Format(
                        LocalizationManager.Instance["Main_ReportSavedFormat"],
                        Path.Combine(directory, ComfyUILibs.Services.CaptioningService.ReportFileName)));
                }

                _snackbarService.Show(
                    LocalizationManager.Instance["Common_Completed"],
                    SummaryText,
                    ControlAppearance.Success,
                    new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                    TimeSpan.FromSeconds(4.0));
            }
            catch (ComfyUIException ex)
            {
                errorMessage = ex.Message;

                _snackbarService.Show(
                    LocalizationManager.Instance["Common_Error"],
                    ex.Message,
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24),
                    TimeSpan.FromSeconds(5.0));
            }
            finally
            {
                await SaveResultLogAsync(directory, status, processed, skipped, errors, errorMessage, usedConfig);

                // WdV3Timm バックエンド使用時、_taggerRunner は実行のたびに常駐サーバープロセスを
                // 起動している。ページを再訪しない限り TryLoadRunnerAsync が呼ばれず、次回実行時に
                // 破棄済みのプロセスへリクエストを送ってしまう（かつプロセス自体が残り続ける）ため、
                // 実行完了のたびに破棄・再構築しておく。ComfyUI バックエンド（Wd14TaggerRunner）は
                // IAsyncDisposable ではないため実質的な影響はない。
                await TryLoadRunnerAsync(showErrorSnackbar: false);

                IsRunning = false;
            }
        }

        /// <summary>1 ファイル処理するたびに呼び出され、進捗表示・ログを更新する。</summary>
        private void OnProgress(CaptioningProgress progress)
        {
            ProgressCurrent = progress.Current;
            ProgressTotal = progress.Total;

            var status = progress.Result switch
            {
                CaptioningResult.Processed => LocalizationManager.Instance["Main_StatusOk"],
                CaptioningResult.Skipped => LocalizationManager.Instance["Main_StatusSkip"],
                CaptioningResult.Error => LocalizationManager.Instance["Main_StatusError"],
                _ => progress.Result.ToString(),
            };

            var line = $"[{progress.Current}/{progress.Total}] {progress.FileName} → {status}";
            if (progress.Result == CaptioningResult.Error && !string.IsNullOrEmpty(progress.ErrorMessage))
                line += $" ({progress.ErrorMessage})";

            ProgressText = line;
            LogEntries.Add(line);
        }

        /// <summary>
        /// captioning_config.json を読み込み、prepend_tags/exclude_tags を今回の実行で実際に使用した
        /// マージ結果（既定値 + MainPage 入力値の union）に差し替えた <see cref="WorkflowConfig"/> を返す。
        /// </summary>
        private async Task<WorkflowConfig> LoadConfigWithMergedTagsAsync(
            IReadOnlyList<string> prependTags, IReadOnlyList<string> excludeTags)
        {
            var json = await File.ReadAllTextAsync(Config.Data.ConfigPath, Encoding.UTF8);
            var config = JsonSerializer.Deserialize<WorkflowConfig>(json, ConfigReadOptions) ?? new WorkflowConfig();
            config.PrependTags = prependTags.ToList();
            config.ExcludeTags = excludeTags.ToList();
            return config;
        }

        /// <summary>
        /// <paramref name="config"/>（<see cref="LoadConfigWithMergedTagsAsync"/> で構築済みのもの）を
        /// 対象ディレクトリ直下へ <see cref="ConfigResultFileName"/> として出力する（今回の実行に使用した設定の記録）。
        /// </summary>
        private async Task SaveExecutedConfigAsync(string directory, WorkflowConfig config)
        {
            var outputPath = Path.Combine(directory, ConfigResultFileName);
            var outputJson = JsonSerializer.Serialize(config, ConfigWriteOptions);
            await File.WriteAllTextAsync(outputPath, outputJson, Encoding.UTF8);

            LogEntries.Add(string.Format(
                LocalizationManager.Instance["Main_ConfigResultSavedFormat"], outputPath));
        }

        /// <summary>
        /// 実行ログ（1 ファイルごとの処理結果）と今回使用した設定をマージした結果ログを、
        /// <see cref="AppConfig.ResultsFolder"/> 配下へ captioning_result_{timestamp}.json として出力する。
        /// ComfyUIRunWorkflow の result_*.json/tag_result_*.json と同じ方針で、成功・失敗どちらの場合も出力する
        /// （<see cref="RunAsync"/> の finally から呼び出す）。ResultsFolder が未設定、または保存自体が
        /// 失敗した場合は実行結果に影響させない。
        /// </summary>
        private async Task SaveResultLogAsync(
            string directory, string status, int processed, int skipped, int errors,
            string? errorMessage, WorkflowConfig? config)
        {
            var resultsFolder = Config.Data.ResultsFolder;
            if (string.IsNullOrWhiteSpace(resultsFolder))
                return;

            try
            {
                Directory.CreateDirectory(resultsFolder);

                var timestamp = DateTime.Now;
                var log = new CaptioningResultLog(
                    Status: status,
                    Timestamp: timestamp,
                    Directory: directory,
                    Recursive: Recursive,
                    Processed: processed,
                    Skipped: skipped,
                    Errors: errors,
                    Error: errorMessage,
                    LogEntries: LogEntries.ToList(),
                    Config: config);

                var outputPath = Path.Combine(resultsFolder, $"captioning_result_{timestamp:yyyyMMdd_HHmmss}.json");
                var outputJson = JsonSerializer.Serialize(log, ConfigWriteOptions);
                await File.WriteAllTextAsync(outputPath, outputJson, Encoding.UTF8);

                LogEntries.Add(string.Format(LocalizationManager.Instance["Main_ResultLogSavedFormat"], outputPath));
            }
            catch
            {
                // 保存失敗は実行結果に影響させない（ComfyUIRunWorkflow の TrySaveResultAsync と同じ方針）
            }
        }

        /// <summary>カンマ区切りタグ文字列を trim・空要素除去したリストに分割する。</summary>
        private static List<string> SplitTags(string text)
            => text.Split(',')
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .ToList();

        /// <summary>
        /// captioning_config.json の既定タグ（Wd14TaggerRunner.PrependTags/ExcludeTags）と MainPage 入力タグを
        /// union する（既定値を先頭、大文字小文字無視で重複排除）。
        /// 同じタグが両方に指定された場合に、タグフィルタ適用後の出力へ二重に挿入されるのを防ぐ。
        /// </summary>
        private static List<string> MergeTags(IReadOnlyList<string> defaults, string extraText)
            => MergeTagLists(defaults, SplitTags(extraText));

        /// <summary>2 つのタグリストを順番通りに連結し、大文字小文字無視で重複排除する（先に現れた方を残す）。</summary>
        private static List<string> MergeTagLists(IReadOnlyList<string> first, IReadOnlyList<string> second)
        {
            var merged = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tag in first.Concat(second))
            {
                if (seen.Add(tag))
                    merged.Add(tag);
            }
            return merged;
        }

        /// <summary>
        /// <see cref="System.Progress{T}"/> と異なり、Report を呼び出したスレッドで同期的にコールバックを実行する。
        /// </summary>
        private sealed class SynchronousProgress<T> : IProgress<T>
        {
            private readonly Action<T> _handler;
            public SynchronousProgress(Action<T> handler) => _handler = handler;
            public void Report(T value) => _handler(value);
        }
    }
}
