using ComfyUICaptioningTool.Services;
using ComfyUILibs.Exceptions;
using ComfyUILibs.Models;

namespace ComfyUICaptioningToolTests.Fakes
{
    /// <summary>
    /// テスト用の <see cref="ICaptioningService"/> スタブ。実ネットワーク通信を行わず、
    /// あらかじめ設定した進捗・結果を返す。呼び出し引数も記録する。
    /// </summary>
    internal class FakeCaptioningService : ICaptioningService
    {
        public (int Processed, int Skipped, int Errors) Result { get; set; } = (0, 0, 0);
        public List<CaptioningProgress> ProgressToReport { get; set; } = new();
        public bool ThrowOnProcessDirectory { get; set; }
        public string ThrowMessage { get; set; } = "エラー";

        public string? ProcessDirectoryArgDirectory { get; private set; }
        public bool ProcessDirectoryArgRecursive { get; private set; }
        public bool ProcessDirectoryArgOverwrite { get; private set; }

        public bool GenerateReportCalled { get; private set; }
        public string? GenerateReportArgDirectory { get; private set; }
        public bool GenerateReportArgRecursive { get; private set; }

        public Task<(int Processed, int Skipped, int Errors)> ProcessDirectoryAsync(
            string directory, bool recursive, bool overwrite, IProgress<CaptioningProgress>? progress = null)
        {
            ProcessDirectoryArgDirectory = directory;
            ProcessDirectoryArgRecursive = recursive;
            ProcessDirectoryArgOverwrite = overwrite;

            if (ThrowOnProcessDirectory)
                throw new ComfyUIException(ThrowMessage);

            foreach (var p in ProgressToReport)
                progress?.Report(p);

            return Task.FromResult(Result);
        }

        public Task GenerateReportAsync(string directory, bool recursive)
        {
            GenerateReportCalled = true;
            GenerateReportArgDirectory = directory;
            GenerateReportArgRecursive = recursive;
            return Task.CompletedTask;
        }
    }
}
