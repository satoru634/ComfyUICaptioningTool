using System.Diagnostics;
using System.IO;
using System.Threading.Channels;
using ComfyUICaptioningTool.Helpers;
using ComfyUILibs.Services;

namespace ComfyUICaptioningTool.Services
{
    /// <summary>
    /// <see cref="IWdV3TimmBuildService"/> の既定実装。<see cref="WdV3TimmPaths.RootDirectory"/>
    /// （既定はアプリと同階層の wdv3-timm フォルダ）同梱の setup.bat・build_exe.bat を
    /// <c>cmd.exe /c</c> 経由で起動し、標準出力・標準エラー出力を1行ずつ通知する。
    /// </summary>
    public class WdV3TimmBuildService : IWdV3TimmBuildService
    {
        private readonly string _rootDirectory;

        /// <summary>本番用コンストラクター。<see cref="WdV3TimmPaths.RootDirectory"/> を使用する。</summary>
        public WdV3TimmBuildService() : this(WdV3TimmPaths.RootDirectory)
        {
        }

        /// <summary>テスト用コンストラクター。wdv3-timm フォルダのパスを直接指定する。</summary>
        public WdV3TimmBuildService(string rootDirectory)
        {
            _rootDirectory = rootDirectory;
        }

        /// <inheritdoc/>
        public bool IsExeReady => File.Exists(Path.Combine(_rootDirectory, "wdv3_timm.exe"));

        /// <inheritdoc/>
        public async Task<bool> BuildAsync(IProgress<string> outputProgress, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(_rootDirectory))
            {
                outputProgress.Report(string.Format(
                    LocalizationManager.Instance["Settings_WdV3TimmBuildRootNotFoundFormat"], _rootDirectory));
                return false;
            }

            if (!await RunScriptAsync("setup.bat", outputProgress, cancellationToken))
                return false;

            if (!await RunScriptAsync("build_exe.bat", outputProgress, cancellationToken))
                return false;

            return true;
        }

        /// <summary>
        /// <paramref name="scriptFileName"/>（<see cref="_rootDirectory"/> 直下の .bat ファイル）を
        /// <c>cmd.exe /c</c> 経由で起動し、完了まで待機する。標準出力・標準エラー出力は
        /// 1 行ごとに <paramref name="outputProgress"/> へ通知する。
        /// </summary>
        /// <returns>終了コードが 0 の場合は true。</returns>
        private async Task<bool> RunScriptAsync(
            string scriptFileName, IProgress<string> outputProgress, CancellationToken cancellationToken)
        {
            var scriptPath = Path.Combine(_rootDirectory, scriptFileName);
            if (!File.Exists(scriptPath))
            {
                outputProgress.Report(string.Format(
                    LocalizationManager.Instance["Settings_WdV3TimmBuildScriptNotFoundFormat"], scriptPath));
                return false;
            }

            outputProgress.Report(string.Format(
                LocalizationManager.Instance["Settings_WdV3TimmBuildScriptStartingFormat"], scriptFileName));

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                WorkingDirectory = _rootDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add("/c");
            startInfo.ArgumentList.Add(scriptPath);

            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

            // OutputDataReceived/ErrorDataReceived は AsyncStreamReader が管理するスレッドプールの
            // スレッドから発火し、この非同期メソッドの await チェーン（呼び出し元の
            // SynchronizationContext を捕捉して再開する）とは無関係に実行される。そのためイベント
            // ハンドラーから outputProgress.Report を直接呼ぶと、SettingsViewModel 側の
            // SynchronousProgress<T> が UI スレッド以外から WdV3TimmBuildLogEntries（UI にバインド
            // 済みの ObservableCollection）を変更してしまい、CollectionView の
            // NotSupportedException を引き起こす。Channel でいったん受け取り、この await foreach
            // （呼び出し元のコンテキストで再開される）から Report することで、常に呼び出し元の
            // スレッド（UI スレッド）で Report が呼ばれるようにする。
            var lines = Channel.CreateUnbounded<string>();
            var openStreamCount = 2;

            void OnLineReceived(string? data)
            {
                if (data != null)
                    lines.Writer.TryWrite(data);
                else if (Interlocked.Decrement(ref openStreamCount) == 0)
                    lines.Writer.TryComplete();
            }

            process.OutputDataReceived += (_, e) => OnLineReceived(e.Data);
            process.ErrorDataReceived += (_, e) => OnLineReceived(e.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await foreach (var line in lines.Reader.ReadAllAsync(cancellationToken))
                outputProgress.Report(line);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                outputProgress.Report(string.Format(
                    LocalizationManager.Instance["Settings_WdV3TimmBuildScriptFailedFormat"],
                    scriptFileName, process.ExitCode));
                return false;
            }

            return true;
        }
    }
}
