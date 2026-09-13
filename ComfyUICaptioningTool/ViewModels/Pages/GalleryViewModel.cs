using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Media.Imaging;
using ComfyUICaptioningTool.Helpers;
using ComfyUICaptioningTool.Models;
using ComfyUICaptioningTool.Services;
using ComfyUILibs.Common;
using ComfyUILibs.Exceptions;
using ComfyUILibs.Services;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace ComfyUICaptioningTool.ViewModels.Pages
{
    /// <summary>
    /// GalleryPage の ViewModel。任意ディレクトリ内の画像と、同名 .txt から読み込んだタグを
    /// カード一覧として表示する。ComfyUI との通信は行わないため（ファイルシステムの走査のみ）、
    /// ConfigPage/ReportPage が使う ICaptioningService ファクトリー境界は不要。
    /// ただし一括タグ操作の入力候補（<see cref="TagList"/>）は <see cref="TagReportGenerator"/> 経由で
    /// tags_report.txt から取得するため、この用途に限り ICaptioningService ファクトリー・Wd14TaggerRunner
    /// （ConfigPath 由来）に依存する。
    /// </summary>
    public partial class GalleryViewModel : ObservableObject, INavigationAware
    {
        /// <summary>タグ付け対象とみなす画像ファイルの拡張子（大文字小文字は無視）。
        /// ComfyUILibs.Services.CaptioningService の同名一覧と揃えているが internal のため参照できず、
        /// GUI 側の表示専用ロジックとしてここに複製している。</summary>
        private static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        /// <summary>サムネイルのデコード幅（px）。メモリ使用量を抑えるため縮小デコードする。</summary>
        private const int ThumbnailDecodePixelWidth = 200;

        /// <summary>アプリケーション設定。</summary>
        public Setting<AppConfig> Config { get; }

        /// <summary>スナックバー通知サービス（gallery_edit_log.jsonl 復元完了時の通知に使用）。</summary>
        private readonly ISnackbarService _snackbarService;

        /// <summary>選択中の対象ディレクトリ。</summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
        private string? _targetDirectory;

        /// <summary>サブディレクトリも対象に含めるか。</summary>
        [ObservableProperty]
        private bool _recursive;

        /// <summary>読み込み中かどうか。</summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
        private bool _isLoading;

        /// <summary>状態メッセージ（ディレクトリ未存在・画像 0 件のいずれか。正常時は空文字）。</summary>
        [ObservableProperty]
        private string _statusMessage = "";

        /// <summary>読み込んだ画像とタグの一覧（ファイル名昇順）。</summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(BulkAddTagCommand))]
        [NotifyCanExecuteChangedFor(nameof(BulkAddTagToStartCommand))]
        [NotifyCanExecuteChangedFor(nameof(BulkRemoveTagCommand))]
        private ObservableCollection<GalleryImageEntry> _images = new();

        /// <summary>タイル一覧で選択中の画像（右ペインにタグ一覧・編集 UI を表示する対象）。未選択時は null。</summary>
        [ObservableProperty]
        private GalleryImageEntry? _selectedImage;

        /// <summary>
        /// 一括タグ操作（<see cref="BulkAddTagCommand"/>/<see cref="BulkAddTagToStartCommand"/>/
        /// <see cref="BulkRemoveTagCommand"/>）の対象タグ入力欄。
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(BulkAddTagCommand))]
        [NotifyCanExecuteChangedFor(nameof(BulkAddTagToStartCommand))]
        [NotifyCanExecuteChangedFor(nameof(BulkRemoveTagCommand))]
        private string _bulkTagInput = "";

        /// <summary>一括タグ操作の AutoSuggestBox で候補表示するタグ一覧（tags_report.txt 由来）。</summary>
        [ObservableProperty]
        private ObservableCollection<string> _tagList = new();

        /// <summary>
        /// <see cref="ITaggerRunner"/> と prepend/exclude タグから <see cref="ICaptioningService"/> を生成するファクトリー。
        /// TagList 更新（<see cref="TagReportGenerator"/> 呼び出し）のみに使うため prepend/exclude タグは常に空リストで呼び出す。
        /// </summary>
        private readonly Func<ITaggerRunner, IReadOnlyList<string>, IReadOnlyList<string>, ICaptioningService> _captioningServiceFactory;

        /// <summary>Config.Data.ConfigPath と Config.Data.TaggerBackend から読み込んだ ITaggerRunner。
        /// 読み込み失敗・未設定時は null。</summary>
        private ITaggerRunner? _taggerRunner;

        /// <summary>
        /// gallery_edit_log.jsonl の復元前に、対象件数を渡してユーザーへ確認するデリゲート。
        /// テスト用の差し替え口（省略時は Wpf.Ui.Controls.MessageBox による確認ダイアログを表示する既定実装）。
        /// </summary>
        private readonly Func<int, Task<bool>> _confirmRestoreEditLogAsync;

        /// <summary>
        /// DI コンテナから設定・スナックバーサービスを受け取って初期化する。
        /// <paramref name="captioningServiceFactory"/> はテスト用の差し替え口（省略時は実ネットワーク通信を行う既定実装）。
        /// <paramref name="confirmRestoreEditLogAsync"/> も同様にテスト用の差し替え口（省略時は確認ダイアログを表示する既定実装）。
        /// </summary>
        public GalleryViewModel(
            Setting<AppConfig> config,
            ISnackbarService snackbarService,
            Func<ITaggerRunner, IReadOnlyList<string>, IReadOnlyList<string>, ICaptioningService>? captioningServiceFactory = null,
            Func<int, Task<bool>>? confirmRestoreEditLogAsync = null)
        {
            Config = config;
            _snackbarService = snackbarService;
            _captioningServiceFactory = captioningServiceFactory
                ?? ((runner, prepend, exclude) => new CaptioningServiceAdapter(runner, prepend, exclude));
            _confirmRestoreEditLogAsync = confirmRestoreEditLogAsync ?? ShowRestoreEditLogConfirmDialogAsync;
        }

        // ── INavigationAware ─────────────────────────────────────────────────

        /// <summary>ページへ遷移するたびに captioning_config.json を再読み込みし、Runner を初期化する。</summary>
        public Task OnNavigatedToAsync() => TryLoadRunnerAsync();

        /// <summary>ページから離れるときは何もしない。</summary>
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        /// <summary>
        /// 設定ページで指定された ConfigPath・TaggerBackend から ITaggerRunner を初期化する。TagList の取得にのみ
        /// 使うため、失敗しても画像・タグ一覧本体の表示には影響させない（スナックバー等のエラー表示は行わない）。
        /// 既存の Runner が <see cref="IAsyncDisposable"/>（WdV3TimmTaggerRunner）の場合、常駐プロセスを
        /// 起動したままリークしないよう再構築前に破棄する（ReportViewModel と同じ理由、実際には
        /// TagAsync を呼ばないため常駐プロセスが起動することは無い）。
        /// </summary>
        private async Task TryLoadRunnerAsync()
        {
            if (_taggerRunner is IAsyncDisposable disposableRunner)
                await disposableRunner.DisposeAsync();

            var path = Config.Data.ConfigPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                _taggerRunner = null;
                return;
            }

            try
            {
                _taggerRunner = TaggerRunnerFactory.Create(path, Config.Data.TaggerBackend);
            }
            catch (ComfyUIException)
            {
                _taggerRunner = null;
            }
        }

        // ── タグ候補一覧（TagList）の更新 ──────────────────────────────────────

        /// <summary>
        /// 対象ディレクトリの tags_report.txt を <see cref="TagReportGenerator"/> で生成・解析し、
        /// <see cref="TagList"/> を更新する。Runner 未読み込み・対象ディレクトリ未設定/未存在の場合は何もしない。
        /// 失敗しても TagList は直前の内容のまま保持し、画像・タグ一覧本体の表示には影響させない。
        /// </summary>
        private async Task RefreshTagListAsync()
        {
            if (_taggerRunner is null)
                return;
            if (string.IsNullOrWhiteSpace(TargetDirectory) || !Directory.Exists(TargetDirectory))
                return;

            try
            {
                var service = _captioningServiceFactory(_taggerRunner, Array.Empty<string>(), Array.Empty<string>());
                var entries = await TagReportGenerator.GenerateAsync(service, TargetDirectory, Recursive);
                TagList = new ObservableCollection<string>(entries.Select(e => e.Tag));
            }
            catch
            {
                // タグ候補一覧の更新失敗は画像・タグ一覧本体の表示には影響させない
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

        // ── タイル選択 ───────────────────────────────────────────────────────

        /// <summary>画像タイル（ItemsControl の各項目）クリックで選択中の画像を切り替える。</summary>
        [RelayCommand]
        private void SelectImage(GalleryImageEntry entry) => SelectedImage = entry;

        // ── 画像・タグ読み込み ────────────────────────────────────────────────

        private bool CanLoad() => !IsLoading && !string.IsNullOrWhiteSpace(TargetDirectory);

        /// <summary>対象ディレクトリ内の画像とタグを読み込み、<see cref="Images"/> を更新する。</summary>
        [RelayCommand(CanExecute = nameof(CanLoad))]
        private async Task LoadAsync()
        {
            var directory = TargetDirectory!;

            Images = new ObservableCollection<GalleryImageEntry>();
            SelectedImage = null;
            StatusMessage = "";

            if (!Directory.Exists(directory))
            {
                StatusMessage = string.Format(LocalizationManager.Instance["Gallery_FolderNotFound_Format"], directory);
                return;
            }

            IsLoading = true;

            try
            {
                var entries = await Task.Run(() => CollectEntries(directory, Recursive, RefreshTagListAsync));
                Images = new ObservableCollection<GalleryImageEntry>(entries);

                if (entries.Count == 0)
                    StatusMessage = LocalizationManager.Instance["Gallery_NoImages"];

                await RefreshTagListAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>対象ディレクトリ内の画像ファイルを収集し、同名 .txt のタグとサムネイルを添えて返す（ファイル名昇順）。</summary>
        private static List<GalleryImageEntry> CollectEntries(string directory, bool recursive, Func<Task> onTagsChangedAsync)
        {
            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var imagePaths = Directory.EnumerateFiles(directory, "*", option)
                .Where(f => SupportedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

            var entries = new List<GalleryImageEntry>();
            foreach (var imagePath in imagePaths)
            {
                var txtPath = Path.ChangeExtension(imagePath, ".txt");
                var tags = File.Exists(txtPath)
                    ? SplitTags(File.ReadAllText(txtPath, Encoding.UTF8))
                    : new List<string>();

                entries.Add(new GalleryImageEntry(
                    Path.GetFileName(imagePath), imagePath, tags, TryCreateThumbnail(imagePath), onTagsChangedAsync));
            }

            return entries;
        }

        // ── 一括タグ操作 ──────────────────────────────────────────────────────

        private bool CanBulkEditTag() => Images.Count > 0 && !string.IsNullOrWhiteSpace(BulkTagInput);

        /// <summary>読み込み済みの全画像に対して、<see cref="BulkTagInput"/> のタグをまとめて末尾に追加し、TagList を更新する。</summary>
        [RelayCommand(CanExecute = nameof(CanBulkEditTag))]
        private async Task BulkAddTagAsync()
        {
            foreach (var entry in Images)
                entry.AddTag(BulkTagInput);

            BulkTagInput = "";
            await RefreshTagListAsync();
        }

        /// <summary>読み込み済みの全画像に対して、<see cref="BulkTagInput"/> のタグをまとめて先頭に追加し、TagList を更新する。</summary>
        [RelayCommand(CanExecute = nameof(CanBulkEditTag))]
        private async Task BulkAddTagToStartAsync()
        {
            foreach (var entry in Images)
                entry.AddTag(BulkTagInput, prepend: true);

            BulkTagInput = "";
            await RefreshTagListAsync();
        }

        /// <summary>
        /// 読み込み済みの全画像から、<see cref="BulkTagInput"/> と一致するタグ（大文字小文字無視）をまとめて削除し、
        /// TagList を更新する。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanBulkEditTag))]
        private async Task BulkRemoveTagAsync()
        {
            var trimmed = BulkTagInput.Trim();
            foreach (var entry in Images)
            {
                var matches = entry.Tags
                    .Where(t => string.Equals(t, trimmed, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                foreach (var match in matches)
                    entry.RemoveTag(match);
            }

            BulkTagInput = "";
            await RefreshTagListAsync();
        }

        // ── 編集ログからの復元 ──────────────────────────────────────────────────

        /// <summary>
        /// ファイル選択ダイアログを開いて gallery_edit_log.jsonl を選ばせる薄いラッパー
        /// （<see cref="MainPageViewModel.ImportTagsFromConfig"/> と同じパターン）。実処理は
        /// <see cref="RestoreEditLogFromFileAsync"/> に分離している。
        /// </summary>
        [RelayCommand]
        private async Task RestoreEditLogAsync()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = LocalizationManager.Instance["Gallery_RestoreEditLogDialogTitle"],
                Filter = "JSON Lines (*.jsonl)|*.jsonl|All files (*.*)|*.*",
            };

            if (dialog.ShowDialog() != true)
                return;

            await RestoreEditLogFromFileAsync(dialog.FileName);
        }

        /// <summary>
        /// 指定した gallery_edit_log.jsonl から、読み込み済みの <see cref="Images"/> のうちファイル名が
        /// 一致するエントリの操作を記録順に再生する。既存のタグ状態はクリアせず、
        /// <see cref="GalleryImageEntry.ApplyEditLogEntry"/>（内部で AddTag/RemoveTag 等をそのまま呼び出す）
        /// をそのまま使うため、通常のタグ編集と同様に .txt・captioning_config_result.json・
        /// gallery_edit_log.jsonl への反映も行われる。実行前に対象件数を提示して確認する。
        /// ファイルダイアログの操作を伴わずユニットテストできるよう、コマンド本体から分離した公開メソッド
        /// （<see cref="MainPageViewModel.ImportTagsFromFile"/> と同じ設計）。
        /// </summary>
        public async Task RestoreEditLogFromFileAsync(string path)
        {
            List<GalleryEditLogEntry> entries;
            try
            {
                entries = ParseEditLog(path);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(LocalizationManager.Instance["Gallery_RestoreEditLogReadErrorFormat"], ex.Message);
                return;
            }

            var imagesByFileName = Images.ToDictionary(i => i.FileName, i => i, StringComparer.OrdinalIgnoreCase);
            var applicable = entries.Where(e => imagesByFileName.ContainsKey(e.FileName)).ToList();

            if (applicable.Count == 0)
            {
                StatusMessage = LocalizationManager.Instance["Gallery_RestoreEditLogNoMatchingEntries"];
                return;
            }

            if (!await _confirmRestoreEditLogAsync(applicable.Count))
                return;

            foreach (var entry in applicable)
                imagesByFileName[entry.FileName].ApplyEditLogEntry(entry.Operation, entry.Tags);

            await RefreshTagListAsync();

            _snackbarService.Show(
                LocalizationManager.Instance["Common_Completed"],
                string.Format(LocalizationManager.Instance["Gallery_RestoreEditLogCompletedFormat"], applicable.Count),
                ControlAppearance.Success,
                new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                TimeSpan.FromSeconds(4.0));
        }

        /// <summary>gallery_edit_log.jsonl（JSON Lines 形式）を 1 行ずつパースする。不正な行はスキップする。</summary>
        private static List<GalleryEditLogEntry> ParseEditLog(string path)
        {
            var result = new List<GalleryEditLogEntry>();
            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    var entry = JsonSerializer.Deserialize<GalleryEditLogEntry>(line);
                    if (entry is not null)
                        result.Add(entry);
                }
                catch (JsonException)
                {
                    // 不正な行はスキップする
                }
            }

            return result;
        }

        /// <summary>
        /// 復元前の確認ダイアログの既定実装。Wpf.Ui.Controls.MessageBox を表示し、
        /// 「復元」ボタン（Primary）が押された場合のみ true を返す。Owner にメインウィンドウを設定し、
        /// WindowStartupLocation を CenterOwner にすることで、親ウィンドウの中心に表示する。
        /// </summary>
        private static async Task<bool> ShowRestoreEditLogConfirmDialogAsync(int count)
        {
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = LocalizationManager.Instance["Gallery_RestoreEditLogConfirmTitle"],
                Content = string.Format(LocalizationManager.Instance["Gallery_RestoreEditLogConfirmMessageFormat"], count),
                PrimaryButtonText = LocalizationManager.Instance["Gallery_RestoreEditLogConfirmYes"],
                CloseButtonText = LocalizationManager.Instance["Gallery_RestoreEditLogConfirmCancel"],
                Owner = Application.Current?.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            var result = await messageBox.ShowDialogAsync();
            return result == Wpf.Ui.Controls.MessageBoxResult.Primary;
        }

        /// <summary>カンマ区切りタグ文字列を trim・空要素除去したリストに分割する。</summary>
        private static List<string> SplitTags(string text)
            => text.Split(',')
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .ToList();

        /// <summary>
        /// 画像ファイルから縮小済みサムネイルを生成する。デコードに失敗した場合（画像として不正なファイル等）は
        /// null を返し、呼び出し元では一覧表示自体は継続する。
        /// </summary>
        private static BitmapImage? TryCreateThumbnail(string imagePath)
        {
            try
            {
                var bytes = File.ReadAllBytes(imagePath);
                using var stream = new MemoryStream(bytes);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = ThumbnailDecodePixelWidth;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}
