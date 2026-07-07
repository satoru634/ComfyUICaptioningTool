using ComfyUICaptioningTool.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace ComfyUICaptioningTool.Views.Pages
{
    /// <summary>実行結果・タグ集計レポート表示ページ（現状はテンプレート由来のランダムカラー一覧デモ）。</summary>
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
