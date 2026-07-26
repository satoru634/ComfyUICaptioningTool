using System.IO;
using ComfyUICaptioningTool.Models;
using ComfyUICaptioningTool.Services;
using ComfyUICaptioningTool.ViewModels.Pages;
using ComfyUICaptioningToolTests.Fakes;
using ComfyUILibs.Common;
using ComfyUILibs.Services;
using Wpf.Ui.Controls;

namespace ComfyUICaptioningToolTests.ViewModels.Pages
{
    public class ReportViewModelTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly FakeSnackbarService _fakeSnackbar;

        public ReportViewModelTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
            _fakeSnackbar = new FakeSnackbarService();
            EnsureTemplateFile();
        }

        public void Dispose() => Directory.Delete(_tempDir, recursive: true);

        /// <summary>
        /// Wd14TaggerRunner は AppDomain.CurrentDomain.BaseDirectory/templates を参照するため、
        /// テスト実行ディレクトリにテンプレートファイルを配置しておく（MainPageViewModelTests と同じ回避策）。
        /// </summary>
        private static void EnsureTemplateFile()
        {
            var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates");
            Directory.CreateDirectory(basePath);
            var targetPath = Path.Combine(basePath, "template_wd14_tagger.json");
            if (File.Exists(targetPath))
                return;

            var templateJson = """
                {
                  "1": {
                    "class_type": "LoadImage",
                    "inputs": {"image": ""},
                    "_meta": {"title": "画像を読み込む"}
                  },
                  "2": {
                    "class_type": "WDTimmTagger",
                    "inputs": {
                      "model_name": "",
                      "general_threshold": 0.5,
                      "character_threshold": 0.5
                    },
                    "_meta": {"title": "WD Timm Tagger"}
                  },
                  "3": {
                    "class_type": "PreviewAny",
                    "inputs": {},
                    "_meta": {"title": "プレビュー任意"}
                  }
                }
                """;
            File.WriteAllText(targetPath, templateJson);
        }

        private Setting<AppConfig> CreateSetting()
            => new Setting<AppConfig>(Path.Combine(_tempDir, "setting.json"), onLoad: false);

        private string WriteValidConfigFile()
        {
            var path = Path.Combine(_tempDir, "captioning_config.json");
            File.WriteAllText(path, """
                {
                  "comfyui_url": "http://127.0.0.1:8188",
                  "wd14_tagger": {
                    "model_name": "wd-eva02-large-tagger-v3",
                    "general_threshold": 0.35,
                    "character_threshold": 0.85
                  }
                }
                """);
            return path;
        }

        private string WriteInvalidConfigFile()
        {
            var path = Path.Combine(_tempDir, "invalid_config.json");
            File.WriteAllText(path, """{ "comfyui_url": "http://127.0.0.1:8188" }""");
            return path;
        }

        private ReportViewModel CreateVm(
            Setting<AppConfig>? setting = null,
            Func<Wd14TaggerRunner, IReadOnlyList<string>, IReadOnlyList<string>, ICaptioningService>? factory = null)
            => new ReportViewModel(setting ?? CreateSetting(), _fakeSnackbar, factory);

        /// <summary>有効な ConfigPath を設定した Setting で ViewModel を作成し、OnNavigatedToAsync まで済ませる。</summary>
        private async Task<ReportViewModel> CreateReadyVmAsync(FakeCaptioningService fake, string? reportDirectory = null)
        {
            var setting = CreateSetting();
            setting.Data.ConfigPath = WriteValidConfigFile();
            var vm = CreateVm(setting, (_, _, _) => fake);
            await vm.OnNavigatedToAsync();
            vm.ReportDirectory = reportDirectory ?? _tempDir;
            return vm;
        }

        /// <summary>
        /// WPF コントロール生成に必要な STA スレッドで非同期処理を実行するヘルパー
        /// （MainPageViewModelTests.RunOnSta と同じ）。
        /// </summary>
        private static void RunOnSta(Func<Task> asyncAction)
            => ComfyUICaptioningToolTests.TestSupport.StaTestRunner.Run(asyncAction);

        // ── コンストラクター ───────────────────────────────────────────────────

        [Fact]
        public void Constructor_Config_IsSet()
        {
            var setting = CreateSetting();

            var vm = CreateVm(setting);

            Assert.Same(setting, vm.Config);
        }

        // ── OnNavigatedToAsync / TryLoadRunner ────────────────────────────────

        [Fact]
        public void OnNavigatedToAsync_EmptyConfigPath_SetsIsConfigLoadedFalse()
        {
            var setting = CreateSetting();
            setting.Data.ConfigPath = "";
            var vm = CreateVm(setting);

            RunOnSta(async () => await vm.OnNavigatedToAsync());

            Assert.False(vm.IsConfigLoaded);
        }

        [Fact]
        public void OnNavigatedToAsync_InvalidConfig_SetsIsConfigLoadedFalse()
        {
            var setting = CreateSetting();
            setting.Data.ConfigPath = WriteInvalidConfigFile();
            var vm = CreateVm(setting);

            RunOnSta(async () => await vm.OnNavigatedToAsync());

            Assert.False(vm.IsConfigLoaded);
        }

        [Fact]
        public async Task OnNavigatedToAsync_ValidConfig_SetsIsConfigLoadedTrue()
        {
            var setting = CreateSetting();
            setting.Data.ConfigPath = WriteValidConfigFile();
            var vm = CreateVm(setting);

            await vm.OnNavigatedToAsync();

            Assert.True(vm.IsConfigLoaded);
        }

        [Fact]
        public async Task OnNavigatedFromAsync_ReturnsCompletedTask()
        {
            var vm = CreateVm();

            var task = vm.OnNavigatedFromAsync();
            await task;

            Assert.True(task.IsCompletedSuccessfully);
        }

        // ── GenerateReportCommand CanExecute ──────────────────────────────────

        [Fact]
        public void GenerateReportCommand_CanExecute_FalseWhenConfigNotLoaded()
        {
            var vm = CreateVm();
            vm.ReportDirectory = _tempDir;

            Assert.False(vm.GenerateReportCommand.CanExecute(null));
        }

        [Fact]
        public async Task GenerateReportCommand_CanExecute_FalseWhenDirectoryNotSet()
        {
            var setting = CreateSetting();
            setting.Data.ConfigPath = WriteValidConfigFile();
            var vm = CreateVm(setting);

            await vm.OnNavigatedToAsync();

            Assert.False(vm.GenerateReportCommand.CanExecute(null));
        }

        [Fact]
        public async Task GenerateReportCommand_CanExecute_TrueWhenReady()
        {
            var vm = await CreateReadyVmAsync(new FakeCaptioningService());

            Assert.True(vm.GenerateReportCommand.CanExecute(null));
        }

        // ── GenerateReportCommand 実行 ────────────────────────────────────────

        [Fact]
        public async Task GenerateReportCommand_Execute_CallsServiceWithDirectoryAndRecursive()
        {
            File.WriteAllText(Path.Combine(_tempDir, CaptioningService.ReportFileName), "1girl: 3\n");
            var fake = new FakeCaptioningService();
            var vm = await CreateReadyVmAsync(fake);
            vm.ReportRecursive = true;

            RunOnSta(async () => await vm.GenerateReportCommand.ExecuteAsync(null));

            Assert.True(fake.GenerateReportCalled);
            Assert.Equal(_tempDir, fake.GenerateReportArgDirectory);
            Assert.True(fake.GenerateReportArgRecursive);
        }

        [Fact]
        public async Task GenerateReportCommand_Execute_PopulatesReportEntriesFromFile()
        {
            var reportPath = Path.Combine(_tempDir, CaptioningService.ReportFileName);
            File.WriteAllLines(reportPath, new[] { "1girl: 3", "solo: 2", "blue eyes: 1" });
            var vm = await CreateReadyVmAsync(new FakeCaptioningService());

            RunOnSta(async () => await vm.GenerateReportCommand.ExecuteAsync(null));

            Assert.Equal(3, vm.ReportEntries.Count);
            Assert.Equal(new TagCountEntry("1girl", 3), vm.ReportEntries[0]);
            Assert.Equal(new TagCountEntry("solo", 2), vm.ReportEntries[1]);
            Assert.Equal(new TagCountEntry("blue eyes", 1), vm.ReportEntries[2]);
        }

        [Fact]
        public async Task GenerateReportCommand_Execute_TagNameContainingColon_ParsedCorrectly()
        {
            var reportPath = Path.Combine(_tempDir, CaptioningService.ReportFileName);
            File.WriteAllLines(reportPath, new[] { "rating:general: 5" });
            var vm = await CreateReadyVmAsync(new FakeCaptioningService());

            RunOnSta(async () => await vm.GenerateReportCommand.ExecuteAsync(null));

            Assert.Equal(new TagCountEntry("rating:general", 5), Assert.Single(vm.ReportEntries));
        }

        [Fact]
        public async Task GenerateReportCommand_Execute_ServiceThrows_ShowsDangerSnackbar()
        {
            File.WriteAllText(Path.Combine(_tempDir, CaptioningService.ReportFileName), "");
            var fake = new FakeCaptioningService { ThrowOnGenerateReport = true, ThrowMessage = "書き込み失敗" };
            var vm = await CreateReadyVmAsync(fake);

            RunOnSta(async () => await vm.GenerateReportCommand.ExecuteAsync(null));

            Assert.Contains(_fakeSnackbar.Calls,
                c => c.Appearance == ControlAppearance.Danger && c.Message == "書き込み失敗");
        }

        [Fact]
        public async Task GenerateReportCommand_Execute_ServiceThrows_ResetsIsGeneratingReport()
        {
            var fake = new FakeCaptioningService { ThrowOnGenerateReport = true };
            var vm = await CreateReadyVmAsync(fake);

            RunOnSta(async () => await vm.GenerateReportCommand.ExecuteAsync(null));

            Assert.False(vm.IsGeneratingReport);
        }

        // ── フィルタリング (FilterText / TagList) ─────────────────────────────

        [Fact]
        public async Task GenerateReportCommand_Execute_PopulatesTagListFromReportEntries()
        {
            var reportPath = Path.Combine(_tempDir, CaptioningService.ReportFileName);
            File.WriteAllLines(reportPath, new[] { "1girl: 3", "solo: 2", "blue eyes: 1" });
            var vm = await CreateReadyVmAsync(new FakeCaptioningService());

            RunOnSta(async () => await vm.GenerateReportCommand.ExecuteAsync(null));

            Assert.Equal(new[] { "1girl", "solo", "blue eyes" }, vm.TagList);
        }

        [Fact]
        public async Task FilterText_PartialMatch_FiltersReportEntriesCaseInsensitive()
        {
            var reportPath = Path.Combine(_tempDir, CaptioningService.ReportFileName);
            File.WriteAllLines(reportPath, new[] { "1girl: 3", "solo: 2", "blue eyes: 1" });
            var vm = await CreateReadyVmAsync(new FakeCaptioningService());
            RunOnSta(async () => await vm.GenerateReportCommand.ExecuteAsync(null));

            vm.FilterText = "GIRL";

            Assert.Equal(new TagCountEntry("1girl", 3), Assert.Single(vm.ReportEntries));
        }

        [Fact]
        public async Task FilterText_ClearedToEmpty_RestoresAllReportEntries()
        {
            var reportPath = Path.Combine(_tempDir, CaptioningService.ReportFileName);
            File.WriteAllLines(reportPath, new[] { "1girl: 3", "solo: 2", "blue eyes: 1" });
            var vm = await CreateReadyVmAsync(new FakeCaptioningService());
            RunOnSta(async () => await vm.GenerateReportCommand.ExecuteAsync(null));
            vm.FilterText = "solo";

            vm.FilterText = "";

            Assert.Equal(3, vm.ReportEntries.Count);
        }

        [Fact]
        public async Task FilterText_NoMatch_ReportEntriesEmpty()
        {
            var reportPath = Path.Combine(_tempDir, CaptioningService.ReportFileName);
            File.WriteAllLines(reportPath, new[] { "1girl: 3", "solo: 2" });
            var vm = await CreateReadyVmAsync(new FakeCaptioningService());
            RunOnSta(async () => await vm.GenerateReportCommand.ExecuteAsync(null));

            vm.FilterText = "nonexistent";

            Assert.Empty(vm.ReportEntries);
        }

        [Fact]
        public async Task GenerateReportCommand_Execute_ResetsPreviousFilterText()
        {
            var reportPath = Path.Combine(_tempDir, CaptioningService.ReportFileName);
            File.WriteAllLines(reportPath, new[] { "1girl: 3", "solo: 2" });
            var vm = await CreateReadyVmAsync(new FakeCaptioningService());
            vm.FilterText = "solo";

            RunOnSta(async () => await vm.GenerateReportCommand.ExecuteAsync(null));

            Assert.Equal("", vm.FilterText);
            Assert.Equal(2, vm.ReportEntries.Count);
        }
    }
}
