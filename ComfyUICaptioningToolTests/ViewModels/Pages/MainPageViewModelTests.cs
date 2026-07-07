using System.IO;
using ComfyUICaptioningTool.Models;
using ComfyUICaptioningTool.ViewModels.Pages;
using ComfyUICaptioningToolTests.Fakes;
using ComfyUILibs.Common;

namespace ComfyUICaptioningToolTests.ViewModels.Pages
{
    public class MainPageViewModelTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly FakeSnackbarService _fakeSnackbar;

        public MainPageViewModelTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
            _fakeSnackbar = new FakeSnackbarService();
        }

        public void Dispose()
        {
            Directory.Delete(_tempDir, recursive: true);
        }

        private Setting<AppConfig> CreateSetting()
            => new Setting<AppConfig>(Path.Combine(_tempDir, "setting.json"), onLoad: false);

        private MainPageViewModel CreateVm(Setting<AppConfig>? setting = null)
            => new MainPageViewModel(setting ?? CreateSetting(), _fakeSnackbar);

        // ── コンストラクター ───────────────────────────────────────────────────

        [Fact]
        public void Constructor_Config_IsSet()
        {
            var setting = CreateSetting();

            var vm = CreateVm(setting);

            Assert.Same(setting, vm.Config);
        }

        [Fact]
        public void Constructor_Counter_IsZero()
        {
            var vm = CreateVm();

            Assert.Equal(0, vm.Counter);
        }

        // ── CounterIncrementCommand ───────────────────────────────────────────

        [Fact]
        public void CounterIncrementCommand_Execute_IncrementsCounterByOne()
        {
            var vm = CreateVm();

            vm.CounterIncrementCommand.Execute(null);

            Assert.Equal(1, vm.Counter);
        }

        [Fact]
        public void CounterIncrementCommand_ExecuteTwice_IncrementsCounterByTwo()
        {
            var vm = CreateVm();

            vm.CounterIncrementCommand.Execute(null);
            vm.CounterIncrementCommand.Execute(null);

            Assert.Equal(2, vm.Counter);
        }
    }
}
