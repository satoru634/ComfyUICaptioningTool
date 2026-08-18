using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Windows.Data;
using ComfyUICaptioningTool.Services;
using ComfyUICaptioningToolTests.TestSupport;

namespace ComfyUICaptioningToolTests.Services
{
    /// <summary>
    /// WdV3TimmBuildService のテスト。実際の wdv3-timm（Python・torch 等）には依存せず、
    /// 隔離した一時フォルダに軽量なダミー .bat スクリプトを配置して cmd.exe 経由の
    /// プロセス起動・標準出力ストリーミング・終了コード判定・逐次実行のプラミング自体を検証する。
    /// </summary>
    public class WdV3TimmBuildServiceTests : IDisposable
    {
        private readonly string _rootDir;

        public WdV3TimmBuildServiceTests()
        {
            _rootDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_rootDir);
        }

        public void Dispose() => Directory.Delete(_rootDir, recursive: true);

        /// <summary>テスト用の同期的な IProgress&lt;T&gt; 実装。呼び出しスレッドで即座に記録する。</summary>
        private sealed class RecordingProgress<T> : IProgress<T>
        {
            public List<T> Reports { get; } = new();
            public void Report(T value) => Reports.Add(value);
        }

        private void WriteScript(string fileName, string content)
            => File.WriteAllText(Path.Combine(_rootDir, fileName), content);

        // ── IsExeReady ────────────────────────────────────────────────────────

        [Fact]
        public void IsExeReady_ExeFileExists_ReturnsTrue()
        {
            File.WriteAllText(Path.Combine(_rootDir, "wdv3_timm.exe"), "dummy");
            var service = new WdV3TimmBuildService(_rootDir);

            Assert.True(service.IsExeReady);
        }

        [Fact]
        public void IsExeReady_ExeFileMissing_ReturnsFalse()
        {
            var service = new WdV3TimmBuildService(_rootDir);

            Assert.False(service.IsExeReady);
        }

        // ── BuildAsync: ルートフォルダ／スクリプト欠落 ──────────────────────────

        [Fact]
        public async Task BuildAsync_RootDirectoryMissing_ReturnsFalse()
        {
            var missingDir = Path.Combine(_rootDir, "does-not-exist");
            var service = new WdV3TimmBuildService(missingDir);
            var progress = new RecordingProgress<string>();

            var result = await service.BuildAsync(progress);

            Assert.False(result);
            Assert.Contains(progress.Reports, line => line.Contains(missingDir));
        }

        [Fact]
        public async Task BuildAsync_SetupScriptMissing_ReturnsFalse()
        {
            // setup.bat を配置しない
            var service = new WdV3TimmBuildService(_rootDir);
            var progress = new RecordingProgress<string>();

            var result = await service.BuildAsync(progress);

            Assert.False(result);
            Assert.Contains(progress.Reports, line => line.Contains("setup.bat"));
        }

        // ── BuildAsync: 実プロセスでの逐次実行 ───────────────────────────────────

        [Fact]
        public async Task BuildAsync_BothScriptsSucceed_ReturnsTrue()
        {
            WriteScript("setup.bat", "@echo off\r\necho setup output\r\nexit /b 0\r\n");
            WriteScript("build_exe.bat", "@echo off\r\necho build output\r\nexit /b 0\r\n");
            var service = new WdV3TimmBuildService(_rootDir);
            var progress = new RecordingProgress<string>();

            var result = await service.BuildAsync(progress);

            Assert.True(result);
            Assert.Contains(progress.Reports, line => line.Contains("setup output"));
            Assert.Contains(progress.Reports, line => line.Contains("build output"));
        }

        [Fact]
        public void BuildAsync_ProgressReportedOnCallingStaThread_DoesNotThrowCrossThreadCollectionException()
        {
            // OutputDataReceived/ErrorDataReceived は AsyncStreamReader 専用のスレッドプールのスレッドから
            // 発火する。RecordingProgress のような素の同期 IProgress<T> ではその事実を検証できない
            // （呼び出し元スレッドを問わず単に記録するだけのため）。SettingsViewModel の
            // SynchronousProgress<T> が UI にバインド済みの ObservableCollection<string> へ直接
            // Add するのと同じ状況を、STA スレッド上に作成した CollectionView（WPF の内部監視対象）を
            // 使って再現し、バックグラウンドスレッドから変更した場合に発生する
            // System.NotSupportedException（CollectionView は生成元と異なるスレッドからの
            // SourceCollection 変更をサポートしない）が起きないことを検証する。
            WriteScript("setup.bat", "@echo off\r\nfor /l %%i in (1,1,30) do echo line %%i\r\nexit /b 0\r\n");
            WriteScript("build_exe.bat", "@echo off\r\necho build output\r\nexit /b 0\r\n");
            var service = new WdV3TimmBuildService(_rootDir);

            StaTestRunner.Run(async () =>
            {
                var callingThreadId = Thread.CurrentThread.ManagedThreadId;
                var lines = new ObservableCollection<string>();
                // CollectionViewSource.GetDefaultView は、呼び出し元スレッドの Dispatcher に紐づいた
                // CollectionView を生成する。SettingsPage.xaml の ListBox バインディングが実行時に
                // 生成するのと同じ種類のオブジェクトであり、生成元と異なるスレッドから
                // lines を変更すると同じ例外が発生する。
                var view = CollectionViewSource.GetDefaultView(lines);
                Assert.NotNull(view);

                var progress = new RecordingProgress<string>();
                var wrapped = new ActionProgress<string>(line =>
                {
                    Assert.Equal(callingThreadId, Thread.CurrentThread.ManagedThreadId);
                    lines.Add(line);
                    progress.Reports.Add(line);
                });

                var result = await service.BuildAsync(wrapped);

                Assert.True(result);
                Assert.NotEmpty(lines);
                Assert.Contains(progress.Reports, l => l.Contains("line 1"));
            });
        }

        /// <summary>SettingsViewModel.SynchronousProgress&lt;T&gt; と同じく、Report を同期的にコールバックへ委譲する。</summary>
        private sealed class ActionProgress<T> : IProgress<T>
        {
            private readonly Action<T> _handler;
            public ActionProgress(Action<T> handler) => _handler = handler;
            public void Report(T value) => _handler(value);
        }

        [Fact]
        public async Task BuildAsync_SetupScriptFails_DoesNotRunBuildExeScript()
        {
            WriteScript("setup.bat", "@echo off\r\necho setup failed\r\nexit /b 1\r\n");
            WriteScript("build_exe.bat", "@echo off\r\necho build output\r\nexit /b 0\r\n");
            var service = new WdV3TimmBuildService(_rootDir);
            var progress = new RecordingProgress<string>();

            var result = await service.BuildAsync(progress);

            Assert.False(result);
            Assert.Contains(progress.Reports, line => line.Contains("setup failed"));
            Assert.DoesNotContain(progress.Reports, line => line.Contains("build output"));
        }

        [Fact]
        public async Task BuildAsync_BuildExeScriptFails_ReturnsFalse()
        {
            WriteScript("setup.bat", "@echo off\r\necho setup output\r\nexit /b 0\r\n");
            WriteScript("build_exe.bat", "@echo off\r\necho build failed\r\nexit /b 1\r\n");
            var service = new WdV3TimmBuildService(_rootDir);
            var progress = new RecordingProgress<string>();

            var result = await service.BuildAsync(progress);

            Assert.False(result);
            Assert.Contains(progress.Reports, line => line.Contains("build failed"));
        }
    }
}
