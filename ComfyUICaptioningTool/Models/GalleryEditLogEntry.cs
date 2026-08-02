using System.Text.Json.Serialization;

namespace ComfyUICaptioningTool.Models
{
    /// <summary>
    /// GalleryPage でのタグ操作（追加・削除・並び替え）1 件分の作業ログエントリ。画像と同じディレクトリの
    /// gallery_edit_log.jsonl へ、1 行 1 エントリの JSON Lines 形式で追記される。
    /// </summary>
    /// <param name="Timestamp">操作日時。</param>
    /// <param name="FileName">操作対象の画像ファイル名。</param>
    /// <param name="Operation">
    /// 操作種別。"add_start"（先頭に追加）/ "add_end"（末尾に追加）/ "remove"（削除）/
    /// "reorder_to_start"（選択タグを先頭へ移動）/ "reorder_to_end"（選択タグを末尾へ移動）/
    /// "reorder_up"（選択タグを1つ前へ移動）/ "reorder_down"（選択タグを1つ後ろへ移動）のいずれか。
    /// </param>
    /// <param name="Tags">操作対象のタグ一覧（追加・削除は1件、並び替えは対象となった選択タグ一覧）。</param>
    public sealed record GalleryEditLogEntry(
        [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
        [property: JsonPropertyName("file_name")] string FileName,
        [property: JsonPropertyName("operation")] string Operation,
        [property: JsonPropertyName("tags")] IReadOnlyList<string> Tags);
}
