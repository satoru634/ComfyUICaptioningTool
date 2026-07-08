using ComfyUILibs.Models;

namespace ComfyUICaptioningTool.Services
{
    /// <summary>
    /// <see cref="ComfyUILibs.Services.CaptioningService"/> の公開メソッドのうち
    /// <see cref="ViewModels.Pages.MainPageViewModel"/> が使用する部分だけを抜き出したインターフェース。
    /// 実ネットワーク通信を伴う WD14 Tagger 呼び出しをテスト時に差し替え可能にするための境界。
    /// </summary>
    public interface ICaptioningService
    {
        /// <summary>ディレクトリ内の画像を一括タグ付けする。<see cref="ComfyUILibs.Services.CaptioningService.ProcessDirectoryAsync"/> 参照。</summary>
        Task<(int Processed, int Skipped, int Errors)> ProcessDirectoryAsync(
            string directory,
            bool recursive,
            bool overwrite,
            IProgress<CaptioningProgress>? progress = null);

        /// <summary>タグ集計レポートを生成する。<see cref="ComfyUILibs.Services.CaptioningService.GenerateReportAsync"/> 参照。</summary>
        Task GenerateReportAsync(string directory, bool recursive);
    }
}
