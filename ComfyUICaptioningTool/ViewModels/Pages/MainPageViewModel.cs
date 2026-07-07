using ComfyUICaptioningTool.Models;
using ComfyUILibs.Common;
using Wpf.Ui;

namespace ComfyUICaptioningTool.ViewModels.Pages
{
    public partial class MainPageViewModel : ObservableObject
    {
        /// <summary>アプリケーション設定。</summary>
        public Setting<AppConfig> Config { get; }

        /// <summary>スナックバー通知サービス。ワークフロー実行中のエラー通知などに使用する。</summary>
        private readonly ISnackbarService _snackbarService;

        [ObservableProperty]
        private int _counter = 0;

        /// <summary>
        /// DI コンテナから設定を受け取って初期化する。
        /// 本 ViewModel はシングルトン登録されているため、言語切替時に画像サイズラベルを
        /// 再生成できるよう <see cref="LocalizationManager"/> の変更通知を購読し続ける。
        /// </summary>
        public MainPageViewModel(Setting<AppConfig> config, ISnackbarService snackbarService)
        {
            Config = config;
            _snackbarService = snackbarService;
            //LocalizationManager.Instance.PropertyChanged += (_, _) => RefreshSizeLabels();
        }

        [RelayCommand]
        private void OnCounterIncrement()
        {
            Counter++;
        }
    }
}
