using ComfyUICaptioningTool.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace ComfyUICaptioningTool.Views.Pages
{
    /// <summary>画像・タグ一覧ページ。</summary>
    public partial class GalleryPage : INavigableView<GalleryViewModel>
    {
        /// <summary>このページに対応する ViewModel。</summary>
        public GalleryViewModel ViewModel { get; }

        /// <summary>DI コンテナから ViewModel を受け取って初期化する。</summary>
        public GalleryPage(GalleryViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
