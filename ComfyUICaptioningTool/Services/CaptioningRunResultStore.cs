using ComfyUICaptioningTool.Models;

namespace ComfyUICaptioningTool.Services
{
    /// <summary>
    /// 直近のディレクトリ一括タグ付け実行結果を保持する DI シングルトン。
    /// <see cref="ViewModels.Pages.MainPageViewModel"/> が実行完了のたびに <see cref="LastResult"/> を更新し、
    /// <see cref="ViewModels.Pages.DataViewModel"/> が購読して表示する（ページ間の状態共有用）。
    /// </summary>
    public partial class CaptioningRunResultStore : ObservableObject
    {
        /// <summary>直近の実行結果。まだ一度も実行されていない場合は null。</summary>
        [ObservableProperty]
        private CaptioningRunResult? _lastResult;
    }
}
