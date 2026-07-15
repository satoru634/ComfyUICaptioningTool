using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        /// <summary>
        /// 各カードのタグ一覧用 <see cref="ScrollViewer"/>（入れ子）のマウスホイール処理。
        /// 素の入れ子 ScrollViewer はマウスホイールイベントを常に自身で消費してしまい、
        /// 画像・タグ一覧全体を包む外側の ScrollViewer までイベントがバブルせずスクロールが
        /// 効かなくなるため、内側がスクロール端（先頭/末尾）に達している場合のみ、
        /// イベントを親要素へ手動で転送して外側のスクロールへ伝播させる。
        /// </summary>
        private void TagsScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer)
                return;

            var atTop = scrollViewer.VerticalOffset <= 0;
            var atBottom = scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight;

            if ((e.Delta > 0 && !atTop) || (e.Delta < 0 && !atBottom))
                return;

            e.Handled = true;

            if (scrollViewer.Parent is not UIElement parent)
                return;

            parent.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = MouseWheelEvent,
            });
        }
    }
}
