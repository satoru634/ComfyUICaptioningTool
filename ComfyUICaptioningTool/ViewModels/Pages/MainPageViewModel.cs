using System.Collections.ObjectModel;
using System.IO;
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
        /// <summary>アプリケーション設定。</summary>
        public Setting<AppConfig> Config { get; }

        /// <summary>スナックバー通知サービス。</summary>
        private readonly ISnackbarService _snackbarService;

        /// <summary>
        /// <see cref="Wd14TaggerRunner"/> と prepend/exclude タグから <see cref="ICaptioningService"/> を生成するファクトリー。
        /// 既定はネットワーク通信を伴う実装。テスト時はフェイクに差し替える。
        /// </summary>
        private readonly Func<Wd14TaggerRunner, IReadOnlyList<string>, IReadOnlyList<string>, ICaptioningService> _captioningServiceFactory;

        /// <summary>Config.Data.ConfigPath から読み込んだ Wd14TaggerRunner。読み込み失敗時は null。</summary>
        private Wd14TaggerRunner? _taggerRunner;

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
            Func<Wd14TaggerRunner, IReadOnlyList<string>, IReadOnlyList<string>, ICaptioningService>? captioningServiceFactory = null)
        {
            Config = config;
            _snackbarService = snackbarService;
            _captioningServiceFactory = captioningServiceFactory
                ?? ((runner, prepend, exclude) => new CaptioningServiceAdapter(runner, prepend, exclude));
        }

        // ── INavigationAware ─────────────────────────────────────────────────

        /// <summary>ページへ遷移するたびに workflow_config.json を再読み込みし、Runner を初期化する。</summary>
        public Task OnNavigatedToAsync()
        {
            TryLoadRunner();
            return Task.CompletedTask;
        }

        /// <summary>ページから離れるときは何もしない。</summary>
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        /// <summary>
        /// 設定ページで指定された ConfigPath から Wd14TaggerRunner を初期化する。
        /// 失敗した場合はスナックバーでエラーメッセージを表示し、実行ボタンを無効化する。
        /// </summary>
        private void TryLoadRunner()
        {
            var path = Config.Data.ConfigPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                _taggerRunner = null;
                IsConfigLoaded = false;

                _snackbarService.Show(
                    LocalizationManager.Instance["Common_Error"],
                    LocalizationManager.Instance["Common_ConfigPathNotSet"],
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24),
                    TimeSpan.FromSeconds(3.0));
                return;
            }

            try
            {
                _taggerRunner = new Wd14TaggerRunner(path);
                IsConfigLoaded = true;
            }
            catch (ComfyUIException ex)
            {
                _taggerRunner = null;
                IsConfigLoaded = false;

                _snackbarService.Show(
                    LocalizationManager.Instance["Common_Error"],
                    string.Format(LocalizationManager.Instance["Main_ConfigLoadErrorFormat"], ex.Message),
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24),
                    TimeSpan.FromSeconds(3.0));
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

            try
            {
                var prependTags = SplitTags(PrependTagsText);
                var excludeTags = SplitTags(ExcludeTagsText);
                var service = _captioningServiceFactory(_taggerRunner!, prependTags, excludeTags);

                // System.Progress<T> はコールバックを SynchronizationContext.Post 経由で非同期に配送するため、
                // await 直後に LogEntries/ProgressText の反映を検証するテストが不安定になる。
                // 本 ViewModel の await はすべて UI スレッドのコンテキストを捕捉して再開するため、
                // 同期的に呼び出しても問題ない（かつテストからも決定的に検証できる）。
                var progress = new SynchronousProgress<CaptioningProgress>(OnProgress);
                var (processed, skipped, errors) = await service.ProcessDirectoryAsync(
                    directory, Recursive, Overwrite, progress);

                SummaryText = string.Format(
                    LocalizationManager.Instance["Main_SummaryFormat"], processed, skipped, errors);

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
                _snackbarService.Show(
                    LocalizationManager.Instance["Common_Error"],
                    ex.Message,
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24),
                    TimeSpan.FromSeconds(5.0));
            }
            finally
            {
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

        /// <summary>カンマ区切りタグ文字列を trim・空要素除去したリストに分割する。</summary>
        private static List<string> SplitTags(string text)
            => text.Split(',')
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .ToList();

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
