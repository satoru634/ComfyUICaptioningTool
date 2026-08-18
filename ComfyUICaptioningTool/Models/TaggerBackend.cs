namespace ComfyUICaptioningTool.Models
{
    /// <summary>
    /// タグ付け処理に使用するバックエンドの種別。<see cref="AppConfig.TaggerBackend"/> で選択する。
    /// </summary>
    public enum TaggerBackend
    {
        /// <summary>ComfyUI 経由（WD Timm Tagger カスタムノード）。</summary>
        ComfyUI,

        /// <summary>ローカルの wdv3-timm 常駐プロセス経由（ComfyUI 不要）。</summary>
        WdV3Timm,
    }
}
