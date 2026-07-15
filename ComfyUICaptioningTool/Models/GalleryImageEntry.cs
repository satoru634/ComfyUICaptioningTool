using System.Windows.Media.Imaging;

namespace ComfyUICaptioningTool.Models
{
    /// <summary>
    /// GalleryPage の一覧表示用に、1 枚の画像とその同名 .txt から読み込んだタグ・サムネイルをまとめたもの。
    /// </summary>
    /// <param name="FileName">画像ファイル名（拡張子込み）。</param>
    /// <param name="FullPath">画像ファイルのフルパス。</param>
    /// <param name="Tags">同名 .txt から読み込んだタグ一覧（trim・空要素除去済み）。.txt が存在しない場合は空リスト。</param>
    /// <param name="Thumbnail">縮小済みサムネイル。デコードに失敗した場合は null。</param>
    public record GalleryImageEntry(string FileName, string FullPath, IReadOnlyList<string> Tags, BitmapImage? Thumbnail)
    {
        /// <summary>タグが 1 つ以上存在するか（.txt が存在し、かつ中身が空でないか）。</summary>
        public bool HasTags => Tags.Count > 0;
    }
}
