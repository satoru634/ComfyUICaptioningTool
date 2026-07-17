using System.Collections.ObjectModel;
using System.IO;
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

        /// <summary>直近生成したタグ集計レポートの内容（出現回数の多い順）。</summary>
        public ObservableCollection<TagCountEntry> ReportEntries { get; } = new();

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
            ReportEntries.Clear();
            ReportStatusText = "";

            try
            {
                var service = _captioningServiceFactory(_taggerRunner!, Array.Empty<string>(), Array.Empty<string>());
                var entries = await TagReportGenerator.GenerateAsync(service, directory, ReportRecursive);
                foreach (var entry in entries)
                    ReportEntries.Add(entry);

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
    }
}
