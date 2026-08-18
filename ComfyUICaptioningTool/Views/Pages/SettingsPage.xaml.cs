using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ComfyUICaptioningTool.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace ComfyUICaptioningTool.Views.Pages
{
    /// <summary>設定ページ。テーマ・言語切り替えを提供する。</summary>
    public partial class SettingsPage : INavigableView<SettingsViewModel>
    {
        /// <summary>このページに対応する ViewModel。</summary>
        public SettingsViewModel ViewModel { get; }

        /// <summary>DI コンテナから ViewModel を受け取って初期化する。</summary>
        public SettingsPage(SettingsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }

        /// <summary>
        /// wdv3-timm ビルドログ用 <see cref="ScrollViewer"/>（入れ子）のマウスホイール処理。
        /// GalleryPage.xaml.cs の TagsScrollViewer_PreviewMouseWheel と同じロジック。素の入れ子
        /// ScrollViewer はマウスホイールイベントを常に自身で消費してしまい、ページ全体を包む外側の
        /// ScrollViewer までイベントがバブルせずスクロールが効かなくなるため、内側がスクロール端
        /// （先頭/末尾）に達している場合のみ、イベントを親要素へ手動で転送して外側のスクロールへ伝播させる。
        /// </summary>
        private void WdV3TimmLogScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
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
                RoutedEvent = UIElement.MouseWheelEvent,
            });
        }
    }
}
