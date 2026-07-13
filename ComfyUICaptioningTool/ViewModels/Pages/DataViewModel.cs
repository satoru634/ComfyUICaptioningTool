using ComfyUICaptioningTool.Helpers;
using ComfyUICaptioningTool.Services;

namespace ComfyUICaptioningTool.ViewModels.Pages
{
    /// <summary>
    /// DataPage の ViewModel。直近のディレクトリ一括タグ付け実行結果の表示を担当する。
    /// </summary>
    public partial class DataViewModel : ObservableObject
    {
        /// <summary>直近の実行結果を保持する共有ストア（MainPageViewModel が更新する）。</summary>
        public CaptioningRunResultStore ResultStore { get; }

        /// <summary>直近の実行結果があるかどうか（未実行時は false）。</summary>
        public bool HasLastResult => ResultStore.LastResult is not null;

        /// <summary>直近の実行対象ディレクトリ（未実行時は空文字）。</summary>
        public string LastResultDirectory => ResultStore.LastResult?.Directory ?? "";

        /// <summary>直近の実行日時の表示文字列（未実行時は空文字）。</summary>
        public string LastResultTimestampText => ResultStore.LastResult is { } r
            ? string.Format(LocalizationManager.Instance["Data_LastRunTimestampFormat"], r.Timestamp)
            : "";

        /// <summary>直近の実行サマリ（未実行時は「まだ実行されていません」旨のメッセージ）。</summary>
        public string LastResultSummary => ResultStore.LastResult is { } r
            ? string.Format(LocalizationManager.Instance["Main_SummaryFormat"], r.Processed, r.Skipped, r.Errors)
            : LocalizationManager.Instance["Data_NoResultYet"];

        /// <summary>直近の実行の 1 ファイルごとのログ（未実行時は空リスト）。</summary>
        public IReadOnlyList<string> LastResultLogEntries => ResultStore.LastResult?.LogEntries ?? Array.Empty<string>();

        /// <summary>
        /// DI コンテナから結果共有ストアを受け取って初期化する。
        /// </summary>
        public DataViewModel(CaptioningRunResultStore resultStore)
        {
            ResultStore = resultStore;

            // ResultStore.LastResult が更新されるたびに、そこから導出する表示用プロパティ群を再通知する
            // （MainPage で実行するたびに DataPage 側の表示も自動的に最新化されるようにするため）。
            ResultStore.PropertyChanged += (_, _) => OnPropertyChanged(string.Empty);
        }
    }
}
