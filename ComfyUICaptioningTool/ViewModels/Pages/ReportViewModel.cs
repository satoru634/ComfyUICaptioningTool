using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
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
    /// ReportPage の ViewModel。任意ディレクトリのタグ集計レポート（tags_report.txt）の生成・表示を担当する。
    /// </summary>
    public partial class ReportViewModel : ObservableObject, INavigationAware
    {
        /// <summary>タグ付け対象とみなす画像ファイルの拡張子（大文字小文字は無視）。
        /// ComfyUILibs.Services.CaptioningService の同名一覧と揃えているが internal のため参照できず、
        /// GUI 側の表示専用ロジックとしてここに複製している（GalleryViewModel と同じ方針）。</summary>
        private static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        /// <summary>アプリケーション設定。</summary>
        public Setting<AppConfig> Config { get; }

        /// <summary>スナックバー通知サービス。</summary>
        private readonly ISnackbarService _snackbarService;

        /// <summary>
        /// <see cref="Wd14TaggerRunner"/> と prepend/exclude タグから <see cref="ICaptioningService"/> を生成するファクトリー。
        /// レポート生成のみに使うため prepend/exclude タグは常に空リストで呼び出す。
        /// </summary>
        private readonly Func<Wd14TaggerRunner, IReadOnlyList<string>, IReadOnlyList<string>, ICaptioningService> _captioningServiceFactory;

        /// <summary>Config.Data.ConfigPath から読み込んだ Wd14TaggerRunner。読み込み失敗時は null。</summary>
        private Wd14TaggerRunner? _taggerRunner;

        /// <summary>ConfigPath の読み込みに成功し、レポート生成が実行可能な状態かどうか。</summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
        private bool _isConfigLoaded;

        /// <summary>タグ集計レポートの対象ディレクトリ。</summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
        private string? _reportDirectory;

        /// <summary>サブディレクトリも集計対象に含めるか。</summary>
        [ObservableProperty]
        private bool _reportRecursive;

        /// <summary>レポート生成中かどうか。</summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
        private bool _isGeneratingReport;

        /// <summary>レポート生成結果のステータス文言。</summary>
        [ObservableProperty]
        private string _reportStatusText = "";

        /// <summary>直近生成したタグ集計レポートの内容（<see cref="FilterText"/> によるフィルタ適用後、出現回数の多い順）。</summary>
        public ObservableCollection<TagCountEntry> ReportEntries { get; } = new();

        /// <summary>直近生成したタグ集計レポートの全件（フィルタ適用前）。</summary>
        private List<TagCountEntry> _allReportEntries = new();

        /// <summary>フィルタ入力欄の AutoSuggestBox に表示する、直近生成したレポートのタグ名一覧。</summary>
        public ObservableCollection<string> TagList { get; } = new();

        /// <summary>タグ名でのフィルタ文字列。入力のたびに <see cref="ReportEntries"/> を絞り込む。</summary>
        [ObservableProperty]
        private string _filterText = "";

        partial void OnFilterTextChanged(string value) => ApplyFilter();

        /// <summary>ListView で選択中のタグ。選択が変わるたびに <see cref="TagUsageImages"/> を再読み込みする。</summary>
        [ObservableProperty]
        private TagCountEntry? _selectedTag;

        partial void OnSelectedTagChanged(TagCountEntry? value) => TagUsageLoadTask = LoadTagUsageImagesAsync(value);

        /// <summary>
        /// 直近の <see cref="TagUsageImages"/> 読み込みタスク。<see cref="SelectedTag"/> の変更は
        /// fire-and-forget で処理されるため、テストから完了を待てるようこのプロパティを公開している。
        /// </summary>
        public Task TagUsageLoadTask { get; private set; } = Task.CompletedTask;

        /// <summary><see cref="SelectedTag"/> を使用している画像のファイル名一覧（ファイル名昇順）。</summary>
        public ObservableCollection<string> TagUsageImages { get; } = new();

        /// <summary>
        /// <see cref="_allReportEntries"/> のうち、<see cref="FilterText"/> をタグ名に含むもの（大文字小文字区別なし）のみを
        /// <see cref="ReportEntries"/> へ反映する。
        /// </summary>
        private void ApplyFilter()
        {
            ReportEntries.Clear();

            var filtered = string.IsNullOrWhiteSpace(FilterText)
                ? _allReportEntries
                : _allReportEntries.Where(e => e.Tag.Contains(FilterText, StringComparison.OrdinalIgnoreCase));

            foreach (var entry in filtered)
                ReportEntries.Add(entry);
        }

        /// <summary>
        /// DI コンテナから設定・スナックバーサービスを受け取って初期化する。
        /// <paramref name="captioningServiceFactory"/> はテスト用の差し替え口（省略時は実ネットワーク通信を行う既定実装）。
        /// </summary>
        public ReportViewModel(
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

        /// <summary>ページへ遷移するたびに captioning_config.json を再読み込みし、Runner を初期化する。</summary>
        public Task OnNavigatedToAsync()
        {
            TryLoadRunner();
            return Task.CompletedTask;
        }

        /// <summary>ページから離れるときは何もしない。</summary>
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        /// <summary>
        /// 設定ページで指定された ConfigPath から Wd14TaggerRunner を初期化する。
        /// 失敗した場合はスナックバーでエラーメッセージを表示し、レポート生成ボタンを無効化する。
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

        /// <summary>フォルダー選択ダイアログを開いてレポート対象ディレクトリを選択する。</summary>
        [RelayCommand]
        private void BrowseReportDirectory()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = LocalizationManager.Instance["Main_DirectoryDialogTitle"],
            };

            if (!string.IsNullOrWhiteSpace(ReportDirectory))
                dialog.InitialDirectory = ReportDirectory;

            if (dialog.ShowDialog() == true)
                ReportDirectory = dialog.FolderName;
        }

        // ── タグ集計レポート生成 ──────────────────────────────────────────────

        private bool CanGenerateReport()
            => IsConfigLoaded && !IsGeneratingReport && !string.IsNullOrWhiteSpace(ReportDirectory);

        /// <summary>
        /// 対象ディレクトリのタグ集計レポート（tags_report.txt）を生成し、内容を <see cref="ReportEntries"/> に読み込む。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGenerateReport))]
        private async Task GenerateReportAsync()
        {
            var directory = ReportDirectory!;

            IsGeneratingReport = true;
            _allReportEntries.Clear();
            ReportEntries.Clear();
            TagList.Clear();
            FilterText = "";
            ReportStatusText = "";
            TagUsageImages.Clear();
            SelectedTag = null;

            try
            {
                var service = _captioningServiceFactory(_taggerRunner!, Array.Empty<string>(), Array.Empty<string>());
                var entries = await TagReportGenerator.GenerateAsync(service, directory, ReportRecursive);
                _allReportEntries = entries;
                ApplyFilter();
                foreach (var entry in entries)
                    TagList.Add(entry.Tag);

                var reportPath = Path.Combine(directory, ComfyUILibs.Services.CaptioningService.ReportFileName);
                ReportStatusText = string.Format(
                    LocalizationManager.Instance["Report_ReportGeneratedFormat"], ReportEntries.Count, reportPath);

                _snackbarService.Show(
                    LocalizationManager.Instance["Common_Completed"],
                    ReportStatusText,
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
                IsGeneratingReport = false;
            }
        }

        // ── 選択タグの使用画像一覧 ─────────────────────────────────────────────

        /// <summary>
        /// <paramref name="tag"/> を使用している画像のファイル名を <see cref="ReportDirectory"/> から収集し、
        /// <see cref="TagUsageImages"/> へ反映する。選択解除時（null）や対象ディレクトリ未存在時は空にする。
        /// </summary>
        private async Task LoadTagUsageImagesAsync(TagCountEntry? tag)
        {
            TagUsageImages.Clear();

            if (tag is null || string.IsNullOrWhiteSpace(ReportDirectory) || !Directory.Exists(ReportDirectory))
                return;

            var directory = ReportDirectory;
            var recursive = ReportRecursive;
            var tagName = tag.Tag;

            List<string> fileNames;
            try
            {
                fileNames = await Task.Run(() => CollectImagesUsingTag(directory, recursive, tagName));
            }
            catch
            {
                // 選択タグの使用画像一覧の更新失敗は、レポート本体の表示には影響させない
                return;
            }

            foreach (var fileName in fileNames)
                TagUsageImages.Add(fileName);
        }

        /// <summary>対象ディレクトリ内の画像のうち、同名 .txt に <paramref name="tagName"/>（大文字小文字無視）を
        /// 含むもののファイル名を、ファイル名昇順で返す。</summary>
        private static List<string> CollectImagesUsingTag(string directory, bool recursive, string tagName)
        {
            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var imagePaths = Directory.EnumerateFiles(directory, "*", option)
                .Where(f => SupportedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

            var result = new List<string>();
            foreach (var imagePath in imagePaths)
            {
                var txtPath = Path.ChangeExtension(imagePath, ".txt");
                if (!File.Exists(txtPath))
                    continue;

                var tags = SplitTags(File.ReadAllText(txtPath, Encoding.UTF8));
                if (tags.Any(t => string.Equals(t, tagName, StringComparison.OrdinalIgnoreCase)))
                    result.Add(Path.GetFileName(imagePath));
            }

            return result;
        }

        /// <summary>カンマ区切りタグ文字列を trim・空要素除去したリストに分割する。</summary>
        private static List<string> SplitTags(string text)
            => text.Split(',')
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .ToList();
    }
}
