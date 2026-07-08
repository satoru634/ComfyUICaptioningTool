namespace ComfyUICaptioningTool.Models
{
    /// <summary>tags_report.txt の 1 行（タグ名と出現回数）を表す。DataPage のタグ集計レポート一覧表示に使用する。</summary>
    /// <param name="Tag">タグ名。</param>
    /// <param name="Count">出現回数。</param>
    public record TagCountEntry(string Tag, int Count);
}
