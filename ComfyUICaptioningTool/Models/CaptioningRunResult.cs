namespace ComfyUICaptioningTool.Models
{
    /// <summary>
    /// <see cref="ViewModels.Pages.MainPageViewModel.RunCommand"/> によるディレクトリ一括タグ付けの
    /// 実行結果スナップショット。<see cref="Services.CaptioningRunResultStore"/> 経由で DataPage に共有される。
    /// </summary>
    /// <param name="Timestamp">実行完了日時。</param>
    /// <param name="Directory">処理対象ディレクトリ。</param>
    /// <param name="Recursive">サブディレクトリも処理したか。</param>
    /// <param name="Processed">処理数。</param>
    /// <param name="Skipped">スキップ数。</param>
    /// <param name="Errors">エラー数。</param>
    /// <param name="LogEntries">1 ファイルごとの処理結果ログ（実行時点のスナップショット）。</param>
    public record CaptioningRunResult(
        DateTime Timestamp,
        string Directory,
        bool Recursive,
        int Processed,
        int Skipped,
        int Errors,
        IReadOnlyList<string> LogEntries);
}
