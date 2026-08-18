using ComfyUICaptioningTool.Services;

namespace ComfyUICaptioningToolTests.Fakes
{
    /// <summary>
    /// テスト用の IWdV3TimmBuildService スタブ。実プロセスを起動せず、あらかじめ設定した
    /// 結果・出力行を返す。BuildAsync の呼び出し回数・キャンセルトークンの伝播も記録する。
    /// </summary>
    internal class FakeWdV3TimmBuildService : IWdV3TimmBuildService
    {
        public bool IsExeReady { get; set; }
        public bool BuildResult { get; set; } = true;
        public List<string> OutputLinesToReport { get; set; } = new();
        public int BuildAsyncCallCount { get; private set; }
        public bool BuildAsyncCalledWithCancelledToken { get; private set; }

        public Task<bool> BuildAsync(IProgress<string> outputProgress, CancellationToken cancellationToken = default)
        {
            BuildAsyncCallCount++;
            BuildAsyncCalledWithCancelledToken = cancellationToken.IsCancellationRequested;

            foreach (var line in OutputLinesToReport)
                outputProgress.Report(line);

            return Task.FromResult(BuildResult);
        }
    }
}
