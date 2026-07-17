using System.IO;
using System.Text.RegularExpressions;
using ComfyUICaptioningTool.Models;

namespace ComfyUICaptioningTool.Services
{
    /// <summary>
    /// <see cref="ICaptioningService.GenerateReportAsync"/> でタグ集計レポート（tags_report.txt）を生成し、
    /// その内容を <see cref="TagCountEntry"/> のリストへ変換する。ReportViewModel から抽出し、
    /// GalleryViewModel 等の他 ViewModel からもタグ一覧の取得に再利用できるようにしたもの。
    /// </summary>
    public static class TagReportGenerator
    {
        /// <summary>tags_report.txt の行（"タグ名: 出現回数"）を解析する正規表現。</summary>
        private static readonly Regex ReportLinePattern = new(@"^(.*): (\d+)$");

        /// <summary>
        /// 対象ディレクトリのタグ集計レポートを生成し、内容を <see cref="TagCountEntry"/> のリスト（出現回数の多い順）として返す。
        /// </summary>
        public static async Task<List<TagCountEntry>> GenerateAsync(ICaptioningService service, string directory, bool recursive)
        {
            await service.GenerateReportAsync(directory, recursive);

            var reportPath = Path.Combine(directory, ComfyUILibs.Services.CaptioningService.ReportFileName);
            var lines = await File.ReadAllLinesAsync(reportPath);

            var entries = new List<TagCountEntry>();
            foreach (var line in lines)
            {
                var match = ReportLinePattern.Match(line);
                if (match.Success)
                    entries.Add(new TagCountEntry(match.Groups[1].Value, int.Parse(match.Groups[2].Value)));
            }

            return entries;
        }
    }
}
