using ComfyUICaptioningTool.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace ComfyUICaptioningTool.Views.Pages
{
    /// <summary>タグ集計レポート表示ページ。</summary>
    public partial class ReportPage : INavigableView<ReportViewModel>
    {
        /// <summary>このページに対応する ViewModel。</summary>
        public ReportViewModel ViewModel { get; }

        /// <summary>DI コンテナから ViewModel を受け取って初期化する。</summary>
        public ReportPage(ReportViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
