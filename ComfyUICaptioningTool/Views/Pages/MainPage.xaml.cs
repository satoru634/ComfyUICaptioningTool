using System.Windows.Input;
using ComfyUICaptioningTool.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace ComfyUICaptioningTool.Views.Pages
{
    /// <summary>ディレクトリ一括タグ付け実行ページ。</summary>
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

        /// <summary>オプション行のラベルクリックでもチェックボックスをトグルできるようにする（グリフとラベルを別要素にしたため）。</summary>
        private void RecursiveLabel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) =>
            RecursiveCheckBox.IsChecked = !(RecursiveCheckBox.IsChecked ?? false);

        private void OverwriteLabel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) =>
            OverwriteCheckBox.IsChecked = !(OverwriteCheckBox.IsChecked ?? false);

        private void GenerateReportLabel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) =>
            GenerateReportCheckBox.IsChecked = !(GenerateReportCheckBox.IsChecked ?? false);
    }
}
