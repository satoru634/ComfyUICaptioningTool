using ComfyUICaptioningTool.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace ComfyUICaptioningTool.Views.Pages
{
    /// <summary>実行結果・タグ集計レポート表示ページ。</summary>
    public partial class DataPage : INavigableView<DataViewModel>
    {
        /// <summary>このページに対応する ViewModel。</summary>
        public DataViewModel ViewModel { get; }

        /// <summary>DI コンテナから ViewModel を受け取って初期化する。</summary>
        public DataPage(DataViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
