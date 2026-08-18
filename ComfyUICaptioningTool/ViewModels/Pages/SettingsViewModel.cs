using ComfyUICaptioningTool.Helpers;
using ComfyUICaptioningTool.Models;
using ComfyUICaptioningTool.Services;
using ComfyUILibs.Common;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace ComfyUICaptioningTool.ViewModels.Pages
{
    /// <summary>
    /// 設定ページの ViewModel。テーマ切り替え・タグ付けバックエンド選択・
    /// wdv3-timm 実行環境のビルドの管理を担当する。
    /// </summary>
    public partial class SettingsViewModel : ObservableObject, INavigationAware
    {
        /// <summary>アプリケーション設定。</summary>
        public Setting<AppConfig> Config { get; }

        /// <summary>スナックバー通知サービス。</summary>
        private readonly ISnackbarService _snackbarService;

        /// <summary>
        /// wdv3-timm 実行環境（.venv・wdv3_timm.exe）のビルドを行うサービス。
        /// 既定はネットワーク通信・プロセス起動を伴う実装。テスト時はフェイクに差し替える。
        /// </summary>
        private readonly IWdV3TimmBuildService _wdV3TimmBuildService;

        private bool _isInitialized = false;

        /// <summary>アプリバージョン文字列。</summary>
        [ObservableProperty]
        private string _appVersion = String.Empty;

        /// <summary>選択中のテーマ。変更時に即時適用される。</summary>
        [ObservableProperty]
        private ApplicationTheme _selectedTheme;

        /// <summary>テーマ選択コンボボックスに表示する選択肢。</summary>
        public List<ApplicationTheme> ThemeList { get; } = new List<ApplicationTheme>
        {
            ApplicationTheme.Light,
            ApplicationTheme.Dark
        };

        /// <summary>選択中の表示言語（"ja" / "en"）。変更時に即時適用される。</summary>
        [ObservableProperty]
        private string _selectedLanguage = "ja";

        /// <summary>
        /// 言語選択コンボボックスに表示する選択肢。ラベルは翻訳せず現地語表記のまま固定する。
        /// </summary>
        public List<LanguageOption> LanguageList { get; } = new List<LanguageOption>
        {
            new("ja", "日本語"),
            new("en", "English"),
        };

        /// <summary>選択中のタグ付けバックエンド。変更時に即時 Config へ反映される。</summary>
        [ObservableProperty]
        private TaggerBackend _selectedTaggerBackend;

        /// <summary>バックエンド選択コンボボックスに表示する選択肢。</summary>
        public List<TaggerBackend> TaggerBackendList { get; } = new List<TaggerBackend>
        {
            TaggerBackend.ComfyUI,
            TaggerBackend.WdV3Timm,
        };

        /// <summary>wdv3_timm.exe が固定パスに存在し常駐サーバーモードを起動できる状態かどうか。</summary>
        [ObservableProperty]
        private bool _isWdV3TimmExeReady;

        partial void OnIsWdV3TimmExeReadyChanged(bool value) => OnPropertyChanged(nameof(WdV3TimmStatusText));

        /// <summary>IsWdV3TimmExeReady に応じた状態表示文言。</summary>
        public string WdV3TimmStatusText => IsWdV3TimmExeReady
            ? LocalizationManager.Instance["Settings_WdV3TimmReady"]
            : LocalizationManager.Instance["Settings_WdV3TimmNotReady"];

        /// <summary>wdv3-timm 実行環境のビルドを実行中かどうか。</summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(BuildWdV3TimmCommand))]
        private bool _isBuildingWdV3Timm;

        /// <summary>ビルド実行中の標準出力・標準エラー出力ログ。</summary>
        public ObservableCollection<string> WdV3TimmBuildLogEntries { get; } = new();

        /// <summary>
        /// DI コンテナから設定・スナックバーサービスを受け取って初期化する。
        /// <paramref name="wdV3TimmBuildService"/> はテスト用の差し替え口（省略時は実プロセス起動を行う既定実装）。
        /// </summary>
        public SettingsViewModel(
            Setting<AppConfig> config,
            ISnackbarService snackbarService,
            IWdV3TimmBuildService? wdV3TimmBuildService = null)
        {
            Config = config;
            _snackbarService = snackbarService;
            _wdV3TimmBuildService = wdV3TimmBuildService ?? new WdV3TimmBuildService();
        }

        /// <summary>
        /// ページへナビゲートされたときに呼び出される。テーマ・言語・バックエンド選択は初回のみ初期化するが、
        /// wdv3_timm.exe の準備状態はページを再訪するたびに再確認する（ビルド後の状態変化を反映するため）。
        /// </summary>
        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();

            IsWdV3TimmExeReady = _wdV3TimmBuildService.IsExeReady;
            return Task.CompletedTask;
        }

        /// <summary>ページから離れるときに設定を保存する。</summary>
        public Task OnNavigatedFromAsync()
        {
            Config.Save();
            return Task.CompletedTask;
        }

        /// <summary>設定ファイルの値を各プロパティへ反映し、初期化済みフラグを立てる。</summary>
        private void InitializeViewModel()
        {
            AppVersion = $"ComfyUICaptioningTool - {GetAssemblyVersion()}";
            SelectedTheme = Config.Data.WindowSetting.Theme;
            SelectedLanguage = Config.Data.Language;
            SelectedTaggerBackend = Config.Data.TaggerBackend;
            _isInitialized = true;
        }

        /// <summary>実行アセンブリのバージョン文字列を取得する。</summary>
        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }

        /// <summary>テーマが変更されたとき、設定へ保存してアプリに即時適用する。</summary>
        partial void OnSelectedThemeChanged(ApplicationTheme value)
        {
            Config.Data.WindowSetting.Theme = value;
            ApplicationThemeManager.Apply(value);
        }

        /// <summary>言語が変更されたとき、設定へ保存してアプリに即時適用する（再起動不要）。</summary>
        partial void OnSelectedLanguageChanged(string value)
        {
            Config.Data.Language = value;
            LocalizationManager.Instance.CurrentCulture = new CultureInfo(value);
        }

        /// <summary>
        /// タグ付けバックエンドが変更されたとき、設定へ保存する。次回ページ遷移時（MainPage/ReportPage/GalleryPage の
        /// OnNavigatedToAsync）から新しいバックエンドで ITaggerRunner が再構築される。
        /// </summary>
        partial void OnSelectedTaggerBackendChanged(TaggerBackend value)
        {
            Config.Data.TaggerBackend = value;
        }

        /// <summary>captioning_config.json のファイル選択ダイアログを開く。</summary>
        [RelayCommand]
        private void BrowseConfigPath()
        {
            var dialog = new OpenFileDialog
            {
                Title = LocalizationManager.Instance["Settings_ConfigFileDialogTitle"],
                Filter = LocalizationManager.Instance["Settings_ConfigFileDialogFilter"],
                DefaultDirectory = AppContext.BaseDirectory,
            };

            if (!string.IsNullOrWhiteSpace(Config.Data.ConfigPath))
            {
                var dir = Path.GetDirectoryName(Config.Data.ConfigPath);
                if (!string.IsNullOrEmpty(dir))
                    dialog.InitialDirectory = dir;
            }

            if (dialog.ShowDialog() == true)
                Config.Data.ConfigPath = dialog.FileName;
        }

        /// <summary>実行結果ログ（captioning_result_*.json）の出力先フォルダ選択ダイアログを開く。</summary>
        [RelayCommand]
        private void BrowseResultsFolder()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = LocalizationManager.Instance["Settings_ResultsFolderDialogTitle"],
            };

            if (!string.IsNullOrWhiteSpace(Config.Data.ResultsFolder))
                dialog.InitialDirectory = Config.Data.ResultsFolder;

            if (dialog.ShowDialog() == true)
                Config.Data.ResultsFolder = dialog.FolderName;
        }

        // ── wdv3-timm 実行環境のビルド ─────────────────────────────────────────

        private bool CanBuildWdV3Timm() => !IsBuildingWdV3Timm;

        /// <summary>
        /// wdv3-timm フォルダ同梱の setup.bat・build_exe.bat を順に実行し、.venv・wdv3_timm.exe を構築する。
        /// 実行中の標準出力・標準エラー出力は <see cref="WdV3TimmBuildLogEntries"/> へ1行ずつ追加する。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanBuildWdV3Timm))]
        private async Task BuildWdV3TimmAsync()
        {
            IsBuildingWdV3Timm = true;
            WdV3TimmBuildLogEntries.Clear();

            try
            {
                // System.Progress<T> はコールバックを SynchronizationContext.Post 経由で非同期に配送するため、
                // 完了直後に WdV3TimmBuildLogEntries の反映を検証するテストが不安定になる。
                // 本メソッドの await はすべて UI スレッドのコンテキストを捕捉して再開するため、
                // 同期的に呼び出しても問題ない（MainPageViewModel.SynchronousProgress と同じ理由）。
                var progress = new SynchronousProgress<string>(line => WdV3TimmBuildLogEntries.Add(line));
                var success = await _wdV3TimmBuildService.BuildAsync(progress);

                IsWdV3TimmExeReady = _wdV3TimmBuildService.IsExeReady;

                if (success)
                {
                    _snackbarService.Show(
                        LocalizationManager.Instance["Common_Completed"],
                        LocalizationManager.Instance["Settings_WdV3TimmBuildSuccess"],
                        ControlAppearance.Success,
                        new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                        TimeSpan.FromSeconds(4.0));
                }
                else
                {
                    _snackbarService.Show(
                        LocalizationManager.Instance["Common_Error"],
                        LocalizationManager.Instance["Settings_WdV3TimmBuildFailure"],
                        ControlAppearance.Danger,
                        new SymbolIcon(SymbolRegular.ErrorCircle24),
                        TimeSpan.FromSeconds(5.0));
                }
            }
            finally
            {
                IsBuildingWdV3Timm = false;
            }
        }

        /// <summary>
        /// <see cref="System.Progress{T}"/> と異なり、Report を呼び出したスレッドで同期的にコールバックを実行する
        /// （MainPageViewModel の同名クラスと同じ実装）。
        /// </summary>
        private sealed class SynchronousProgress<T> : IProgress<T>
        {
            private readonly Action<T> _handler;
            public SynchronousProgress(Action<T> handler) => _handler = handler;
            public void Report(T value) => _handler(value);
        }
    }
}
