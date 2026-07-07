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
    }
}
