namespace ComfyUICaptioningTool.Models
{
    /// <summary>
    /// DataPage の一覧表示用に、<see cref="CaptioningResultLog"/>（Results フォルダから読み込んだ 1 件分の実行結果ログ）と
    /// 表示用に整形済みの文字列（日時・サマリ）をまとめたもの。
    /// </summary>
    /// <param name="Log">読み込んだ実行結果ログ本体。</param>
    /// <param name="TimestampText">実行日時の表示文字列。</param>
    /// <param name="SummaryText">成功時はサマリ（処理/スキップ/エラー件数）、失敗時はエラーメッセージ。</param>
    public record CaptioningResultLogPreview(CaptioningResultLog Log, string TimestampText, string SummaryText)
    {
        /// <summary>実行が成功したかどうか（Log.Status == "success"）。</summary>
        public bool IsSuccess => Log.Status == "success";
    }
}
