using ComfyUICaptioningTool.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace ComfyUICaptioningTool.Views.Pages
{
    /// <summary>キャプショニング実行ページ（現状はテンプレート由来のカウンターデモ）。</summary>
    public partial class MainPage : INavigableView<MainPageViewModel>
    {
        /// <summary>このページに対応する ViewModel。</summary>
        public MainPageViewModel ViewModel { get; }

        /// <summary>DI コンテナから ViewModel を受け取って初期化する。</summary>
        public MainPage(MainPageViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
