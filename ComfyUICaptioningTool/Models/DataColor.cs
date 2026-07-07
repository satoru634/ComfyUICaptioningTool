using System.Windows.Media;

namespace ComfyUICaptioningTool.Models
{
    /// <summary>
    /// テンプレート由来のサンプルデータ（DataPage のランダムカラー一覧表示用）。
    /// キャプショニング機能固有の実装に置き換わるまでの暫定モデル。
    /// </summary>
    public struct DataColor
    {
        /// <summary>表示するランダムカラー。</summary>
        public Brush Color { get; set; }
    }
}
