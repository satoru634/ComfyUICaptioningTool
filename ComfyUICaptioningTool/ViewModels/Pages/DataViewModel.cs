using System.Windows.Media;
using ComfyUICaptioningTool.Models;
using Wpf.Ui.Abstractions.Controls;

namespace ComfyUICaptioningTool.ViewModels.Pages
{
    /// <summary>
    /// DataPage の ViewModel。テンプレート由来のランダムカラー一覧デモ。
    /// 処理結果・タグ集計レポート表示 VM に置き換わるまでの暫定実装。
    /// </summary>
    public partial class DataViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        /// <summary>一覧表示するランダムカラーのコレクション。</summary>
        [ObservableProperty]
        private IEnumerable<DataColor> _colors;

        /// <summary>ページへナビゲートされたときに呼び出される。初回のみ初期化する。</summary>
        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();

            return Task.CompletedTask;
        }

        /// <summary>ページから離れるときに呼び出される。本 VM では特に処理を行わない。</summary>
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        /// <summary>表示用にランダムカラーを 8192 件生成し、<see cref="Colors"/> に設定する。</summary>
        private void InitializeViewModel()
        {
            var random = new Random();
            var colorCollection = new List<DataColor>();

            for (int i = 0; i < 8192; i++)
                colorCollection.Add(
                    new DataColor
                    {
                        Color = new SolidColorBrush(
                            Color.FromArgb(
                                (byte)200,
                                (byte)random.Next(0, 250),
                                (byte)random.Next(0, 250),
                                (byte)random.Next(0, 250)
                            )
                        )
                    }
                );

            Colors = colorCollection;

            _isInitialized = true;
        }
    }
}
