using System.Text.Json.Serialization;
using ComfyUILibs.Models;

namespace ComfyUICaptioningTool.Models
{
    /// <summary>
    /// <see cref="ViewModels.Pages.MainPageViewModel.RunCommand"/> によるディレクトリ一括タグ付けの
    /// 実行ログと使用した設定をマージした結果ログ。<c>AppConfig.ResultsFolder</c> 配下に
    /// <c>captioning_result_{timestamp}.json</c> として出力される（ComfyUIRunWorkflow の
    /// result_*.json/tag_result_*.json と同じ方式で、成功・失敗どちらの場合も出力する）。
    /// </summary>
    /// <param name="Status">実行結果のステータス。"success" または "error"。</param>
    /// <param name="Timestamp">実行完了（または失敗）日時。</param>
    /// <param name="Directory">処理対象ディレクトリ。</param>
    /// <param name="Recursive">サブディレクトリも処理したか。</param>
    /// <param name="Processed">処理数。</param>
    /// <param name="Skipped">スキップ数。</param>
    /// <param name="Errors">エラー数。</param>
    /// <param name="Error">実行時例外のメッセージ。成功時は null。</param>
    /// <param name="LogEntries">1 ファイルごとの処理結果ログ（実行時点のスナップショット）。</param>
    /// <param name="Config">今回の実行で実際に使用した設定（prepend_tags/exclude_tags はマージ後の値）。</param>
    public record CaptioningResultLog(
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("timestamp")] DateTime Timestamp,
        [property: JsonPropertyName("directory")] string Directory,
        [property: JsonPropertyName("recursive")] bool Recursive,
        [property: JsonPropertyName("processed")] int Processed,
        [property: JsonPropertyName("skipped")] int Skipped,
        [property: JsonPropertyName("errors")] int Errors,
        [property: JsonPropertyName("error")] string? Error,
        [property: JsonPropertyName("log_entries")] IReadOnlyList<string> LogEntries,
        [property: JsonPropertyName("config")] WorkflowConfig? Config);
}
