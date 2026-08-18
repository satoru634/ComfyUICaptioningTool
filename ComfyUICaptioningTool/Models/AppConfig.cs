using ComfyUILibs.Base;
using System.IO;
using Wpf.Ui.Appearance;

namespace ComfyUICaptioningTool.Models
{
    /// <summary>
    /// ウィンドウの表示状態（位置・サイズ・テーマ・ペイン開閉）を保持するデータクラス。
    /// <see cref="AppConfig"/> の一部として JSON に永続化される。
    /// </summary>
    public partial class WindowSettingData : ObservableObject
    {
        /// <summary>ウィンドウの左上座標（スクリーン座標系）。</summary>
        [ObservableProperty]
        private ObservablePoint _windowPos;

        /// <summary>ウィンドウの幅と高さ（ピクセル）。</summary>
        [ObservableProperty]
        private ObservableSize _windowSize;

        /// <summary>ウィンドウの表示状態（Normal / Minimized / Maximized）。</summary>
        [ObservableProperty]
        private WindowState _state;

        /// <summary>アプリケーションのテーマ（Light / Dark）。</summary>
        [ObservableProperty]
        private ApplicationTheme _theme;

        /// <summary>ナビゲーションペインが開いているかどうか。</summary>
        [ObservableProperty]
        private bool _isPaneOpen;

        /// <summary>
        /// フィールドを既定値で初期化する。JSON デシリアライズ時にも呼ばれるため、
        /// ここでは暫定の値のみを設定し、実際のデフォルト値は <see cref="AppConfig"/> のコンストラクターで上書きする。
        /// </summary>
        public WindowSettingData()
        {
            _windowPos = new ObservablePoint(0, 0);
            _windowSize = new ObservableSize(0, 0);
            _state = WindowState.Normal;
            _theme = ApplicationTheme.Light;
            _isPaneOpen = false;
        }
    }

    /// <summary>
    /// アプリケーション全体の設定を保持するルートクラス。
    /// <c>Setting&lt;AppConfig&gt;</c> 経由で <c>ComfyUICaptioningTool_setting.json</c> に永続化される。
    /// </summary>
    public partial class AppConfig : ObservableObject
    {
        /// <summary>ウィンドウ状態の設定グループ。</summary>
        [ObservableProperty]
        private WindowSettingData _windowSetting;

        /// <summary>GUI の表示言語（"ja" / "en"）。OS ロケールに関わらず既定は "ja"。</summary>
        [ObservableProperty]
        private string _language = "ja";

        /// <summary>
        /// captioning_config.json のパス（comfyui_url・wd14_tagger 設定を含む）。
        /// 初回起動時は実行ファイルと同階層の captioning_config.json を既定値とする。
        /// GUI 内では値を直接編集せず、設定ページのファイル選択ダイアログでパスのみを指定する
        /// （ComfyUIRunWorkflow と同じ方式）。
        /// </summary>
        [ObservableProperty]
        private string _configPath = Path.Combine(AppContext.BaseDirectory, "captioning_config.json");

        /// <summary>
        /// タグ付け実行結果ログ（実行ログ + 使用した設定をマージした JSON）の出力先フォルダ。
        /// ComfyUIRunWorkflow の ResultsFolder と同じ方式で、SettingsPage のフォルダ選択ダイアログで変更できる。
        /// </summary>
        [ObservableProperty]
        private string _resultsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Results");

        /// <summary>
        /// タグ付けに使用するバックエンド。既定は <see cref="TaggerBackend.ComfyUI"/>（既存の挙動を維持）。
        /// SettingsPage で切り替える（<see cref="ComfyUICaptioningTool.Services.TaggerRunnerFactory"/> がこの値に応じて
        /// Wd14TaggerRunner/WdV3TimmTaggerRunner のどちらを構築するかを決定する）。
        /// </summary>
        [ObservableProperty]
        private TaggerBackend _taggerBackend = TaggerBackend.ComfyUI;

        /// <summary>
        /// 初回起動時のデフォルト値を設定する。
        /// 設定ファイルが存在する場合は JSON デシリアライズ後に上書きされる。
        /// </summary>
        public AppConfig()
        {
            _windowSetting = new WindowSettingData();

            _windowSetting.WindowPos = new ObservablePoint(100, 100);
            _windowSetting.WindowSize = new ObservableSize(1000, 800);
            _windowSetting.State = WindowState.Normal;
            _windowSetting.Theme = ApplicationTheme.Light;
            _windowSetting.IsPaneOpen = false;
        }
    }
}
