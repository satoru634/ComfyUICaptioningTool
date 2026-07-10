using System.IO;
using ComfyUICaptioningTool.Helpers;
using ComfyUICaptioningTool.Models;
using ComfyUICaptioningTool.Services;
using ComfyUICaptioningTool.ViewModels.Pages;
using ComfyUICaptioningToolTests.Fakes;
using ComfyUILibs.Common;
using ComfyUILibs.Models;
using ComfyUILibs.Services;
using Wpf.Ui.Controls;

namespace ComfyUICaptioningToolTests.ViewModels.Pages
{
    public class MainPageViewModelTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly FakeSnackbarService _fakeSnackbar;
        private readonly CaptioningRunResultStore _resultStore;

        public MainPageViewModelTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
            _fakeSnackbar = new FakeSnackbarService();
            _resultStore = new CaptioningRunResultStore();
            EnsureTemplateFile();
        }

        public void Dispose() => Directory.Delete(_tempDir, recursive: true);

        /// <summary>
        /// Wd14TaggerRunner は AppDomain.CurrentDomain.BaseDirectory/templates を参照するため、
        /// テスト実行ディレクトリにテンプレートファイルを配置しておく（ComfyUILibsTests と同じ回避策）。
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

        /// <summary>prepend_tags/exclude_tags を含む captioning_config.json を書き出す。</summary>
        private string WriteConfigFileWithTags(IEnumerable<string> prependTags, IEnumerable<string> excludeTags)
        {
            var path = Path.Combine(_tempDir, "captioning_config.json");
            var config = new
            {
                comfyui_url = "http://127.0.0.1:8188",
                wd14_tagger = new
                {
                    model_name = "wd-eva02-large-tagger-v3",
                    general_threshold = 0.35,
                    character_threshold = 0.85
                },
                prepend_tags = prependTags,
                exclude_tags = excludeTags,
            };
            File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(config));
            return path;
        }

        private string WriteInvalidConfigFile()
        {
            var path = Path.Combine(_tempDir, "invalid_config.json");
            File.WriteAllText(path, """{ "comfyui_url": "http://127.0.0.1:8188" }""");
            return path;
        }

        private MainPageViewModel CreateVm(
            Setting<AppConfig>? setting = null,
            Func<Wd14TaggerRunner, IReadOnlyList<string>, IReadOnlyList<string>, ICaptioningService>? factory = null)
            => new MainPageViewModel(setting ?? CreateSetting(), _fakeSnackbar, _resultStore, factory);

        /// <summary>有効な ConfigPath を設定した Setting で ViewModel を作成し、OnNavigatedToAsync まで済ませる。</summary>
        private async Task<MainPageViewModel> CreateReadyVmAsync(FakeCaptioningService fake, string? targetDirectory = null)
        {
            var setting = CreateSetting();
            setting.Data.ConfigPath = WriteValidConfigFile();
            var vm = CreateVm(setting, (_, _, _) => fake);
            await vm.OnNavigatedToAsync();
            vm.TargetDirectory = targetDirectory ?? _tempDir;
            return vm;
        }

        /// <summary>
        /// WPF コントロール生成に必要な STA スレッドで非同期処理を実行するヘルパー。
        /// MainPageViewModel はスナックバー表示時に SymbolIcon（WPF FrameworkElement）を生成するため、
        /// ConfigPath 読み込み失敗時や RunCommand 実行時のテストは STA スレッド上で行う必要がある
        /// （MainWindowViewModelTests.RunOnSta の非同期版）。
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

        [Fact]
        public void Constructor_InitialState_IsNotRunning()
        {
            var vm = CreateVm();

            Assert.False(vm.IsRunning);
        }

        [Fact]
        public void Constructor_InitialState_LogEntriesEmpty()
        {
            var vm = CreateVm();

            Assert.Empty(vm.LogEntries);
        }

        [Fact]
        public void Constructor_InitialState_HasProgressIsFalse()
        {
            var vm = CreateVm();

            Assert.False(vm.HasProgress);
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
        public void OnNavigatedToAsync_EmptyConfigPath_ShowsDangerSnackbar()
        {
            var setting = CreateSetting();
            setting.Data.ConfigPath = "";
            var vm = CreateVm(setting);

            RunOnSta(async () => await vm.OnNavigatedToAsync());

            Assert.Contains(_fakeSnackbar.Calls, c => c.Appearance == ControlAppearance.Danger);
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
        public void OnNavigatedToAsync_InvalidConfig_ShowsDangerSnackbar()
        {
            var setting = CreateSetting();
            setting.Data.ConfigPath = WriteInvalidConfigFile();
            var vm = CreateVm(setting);

            RunOnSta(async () => await vm.OnNavigatedToAsync());

            Assert.Contains(_fakeSnackbar.Calls, c => c.Appearance == ControlAppearance.Danger);
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

        // ── RunCommand CanExecute ─────────────────────────────────────────────

        [Fact]
        public void RunCommand_CanExecute_FalseWhenConfigNotLoaded()
        {
            var vm = CreateVm();
            vm.TargetDirectory = _tempDir;

            Assert.False(vm.RunCommand.CanExecute(null));
        }

        [Fact]
        public async Task RunCommand_CanExecute_FalseWhenDirectoryNotSet()
        {
            var setting = CreateSetting();
            setting.Data.ConfigPath = WriteValidConfigFile();
            var vm = CreateVm(setting);
            await vm.OnNavigatedToAsync();

            Assert.False(vm.RunCommand.CanExecute(null));
        }

        [Fact]
        public async Task RunCommand_CanExecute_TrueWhenConfigLoadedAndDirectorySet()
        {
            var vm = await CreateReadyVmAsync(new FakeCaptioningService());

            Assert.True(vm.RunCommand.CanExecute(null));
        }

        [Fact]
        public async Task RunCommand_CanExecute_FalseWhileRunning()
        {
            var vm = await CreateReadyVmAsync(new FakeCaptioningService());
            vm.IsRunning = true;

            Assert.False(vm.RunCommand.CanExecute(null));
        }

        // ── RunCommand 実行 ────────────────────────────────────────────────────

        [Fact]
        public async Task RunCommand_Execute_ReportsProgressToLogEntries()
        {
            var fake = new FakeCaptioningService
            {
                Result = (2, 0, 0),
                ProgressToReport = new()
                {
                    new CaptioningProgress(1, 2, "a.jpg", CaptioningResult.Processed),
                    new CaptioningProgress(2, 2, "b.jpg", CaptioningResult.Processed),
                }
            };
            var vm = await CreateReadyVmAsync(fake);

            RunOnSta(async () => await vm.RunCommand.ExecuteAsync(null));

            Assert.Equal(2, vm.LogEntries.Count);
            Assert.Contains("a.jpg", vm.LogEntries[0]);
            Assert.Contains("b.jpg", vm.LogEntries[1]);
            Assert.Equal(2, vm.ProgressCurrent);
            Assert.Equal(2, vm.ProgressTotal);
            Assert.True(vm.HasProgress);
        }

        [Fact]
        public async Task RunCommand_Execute_ErrorProgress_IncludesErrorMessageInLog()
        {
            var fake = new FakeCaptioningService
            {
                Result = (0, 0, 1),
                ProgressToReport = new()
                {
                    new CaptioningProgress(1, 1, "bad.jpg", CaptioningResult.Error, "読み込み失敗"),
                }
            };
            var vm = await CreateReadyVmAsync(fake);

            RunOnSta(async () => await vm.RunCommand.ExecuteAsync(null));

            Assert.Contains("読み込み失敗", vm.LogEntries[0]);
        }

        [Fact]
        public async Task RunCommand_Execute_SetsSummaryText()
        {
            var fake = new FakeCaptioningService { Result = (5, 1, 2) };
            var vm = await CreateReadyVmAsync(fake);

            RunOnSta(async () => await vm.RunCommand.ExecuteAsync(null));

            Assert.Equal(
                string.Format(LocalizationManager.Instance["Main_SummaryFormat"], 5, 1, 2),
                vm.SummaryText);
        }

        [Fact]
        public async Task RunCommand_Execute_TogglesIsRunning_BackToFalseAfterCompletion()
        {
            var vm = await CreateReadyVmAsync(new FakeCaptioningService());

            RunOnSta(async () => await vm.RunCommand.ExecuteAsync(null));

            Assert.False(vm.IsRunning);
        }

        [Fact]
        public async Task RunCommand_Execute_Success_UpdatesResultStoreLastResult()
        {
            var fake = new FakeCaptioningService { Result = (3, 1, 0) };
            var vm = await CreateReadyVmAsync(fake);

            RunOnSta(async () => await vm.RunCommand.ExecuteAsync(null));

            var result = _resultStore.LastResult;
            Assert.NotNull(result);
            Assert.Equal(_tempDir, result!.Directory);
            Assert.Equal(3, result.Processed);
            Assert.Equal(1, result.Skipped);
            Assert.Equal(0, result.Errors);
        }

        [Fact]
        public async Task RunCommand_Execute_ServiceThrows_DoesNotUpdateResultStore()
        {
            var fake = new FakeCaptioningService { ThrowOnProcessDirectory = true };
            var vm = await CreateReadyVmAsync(fake);

            RunOnSta(async () => await vm.RunCommand.ExecuteAsync(null));

            Assert.Null(_resultStore.LastResult);
        }

        [Fact]
        public async Task RunCommand_Execute_PassesDirectoryRecursiveOverwriteToService()
        {
            var fake = new FakeCaptioningService();
            var vm = await CreateReadyVmAsync(fake);
            vm.Recursive = true;
            vm.Overwrite = true;

            RunOnSta(async () => await vm.RunCommand.ExecuteAsync(null));

            Assert.Equal(_tempDir, fake.ProcessDirectoryArgDirectory);
            Assert.True(fake.ProcessDirectoryArgRecursive);
            Assert.True(fake.ProcessDirectoryArgOverwrite);
        }

        [Fact]
        public async Task RunCommand_Execute_PassesParsedPrependAndExcludeTags()
        {
            var fake = new FakeCaptioningService();
            IReadOnlyList<string>? capturedPrepend = null;
            IReadOnlyList<string>? capturedExclude = null;

            var setting = CreateSetting();
            setting.Data.ConfigPath = WriteValidConfigFile();
            var vm = CreateVm(setting, (_, prepend, exclude) =>
            {
                capturedPrepend = prepend;
                capturedExclude = exclude;
                return fake;
            });
            await vm.OnNavigatedToAsync();
            vm.TargetDirectory = _tempDir;
            vm.PrependTagsText = " my_chara ,1girl";
            vm.ExcludeTagsText = "rating:general, ";

            RunOnSta(async () => await vm.RunCommand.ExecuteAsync(null));

            Assert.Equal(new[] { "my_chara", "1girl" }, capturedPrepend);
            Assert.Equal(new[] { "rating:general" }, capturedExclude);
        }

        [Fact]
        public async Task RunCommand_Execute_MergesConfigTagsBeforeInputTags()
        {
            var fake = new FakeCaptioningService();
            IReadOnlyList<string>? capturedPrepend = null;
            IReadOnlyList<string>? capturedExclude = null;

            var setting = CreateSetting();
            setting.Data.ConfigPath = WriteConfigFileWithTags(
                new[] { "my_chara" }, new[] { "rating:general" });
            var vm = CreateVm(setting, (_, prepend, exclude) =>
            {
                capturedPrepend = prepend;
                capturedExclude = exclude;
                return fake;
            });
            await vm.OnNavigatedToAsync();
            vm.TargetDirectory = _tempDir;
            vm.PrependTagsText = "1girl";
            vm.ExcludeTagsText = "solo";

            RunOnSta(async () => await vm.RunCommand.ExecuteAsync(null));

            Assert.Equal(new[] { "my_chara", "1girl" }, capturedPrepend);
            Assert.Equal(new[] { "rating:general", "solo" }, capturedExclude);
        }

        [Fact]
        public async Task RunCommand_Execute_MergesConfigAndInputTags_DeduplicatesCaseInsensitive()
        {
            var fake = new FakeCaptioningService();
            IReadOnlyList<string>? capturedPrepend = null;

            var setting = CreateSetting();
            setting.Data.ConfigPath = WriteConfigFileWithTags(
                new[] { "my_chara" }, Array.Empty<string>());
            var vm = CreateVm(setting, (_, prepend, _) =>
            {
                capturedPrepend = prepend;
                return fake;
            });
            await vm.OnNavigatedToAsync();
            vm.TargetDirectory = _tempDir;
            vm.PrependTagsText = "MY_CHARA, 1girl";

            RunOnSta(async () => await vm.RunCommand.ExecuteAsync(null));

            Assert.Equal(new[] { "my_chara", "1girl" }, capturedPrepend);
        }

        [Fact]
        public async Task RunCommand_Execute_GenerateReportTrue_CallsGenerateReportAsync()
        {
            var fake = new FakeCaptioningService();
            var vm = await CreateReadyVmAsync(fake);
            vm.GenerateReport = true;

            RunOnSta(async () => await vm.RunCommand.ExecuteAsync(null));

            Assert.True(fake.GenerateReportCalled);
            Assert.Equal(_tempDir, fake.GenerateReportArgDirectory);
        }

        [Fact]
        public async Task RunCommand_Execute_GenerateReportFalse_DoesNotCallGenerateReportAsync()
        {
            var fake = new FakeCaptioningService();
            var vm = await CreateReadyVmAsync(fake);
            vm.GenerateReport = false;

            RunOnSta(async () => await vm.RunCommand.ExecuteAsync(null));

            Assert.False(fake.GenerateReportCalled);
        }

        [Fact]
        public async Task RunCommand_Execute_Success_ShowsSuccessSnackbar()
        {
            var fake = new FakeCaptioningService { Result = (1, 0, 0) };
            var vm = await CreateReadyVmAsync(fake);

            RunOnSta(async () => await vm.RunCommand.ExecuteAsync(null));

            Assert.Contains(_fakeSnackbar.Calls, c => c.Appearance == ControlAppearance.Success);
        }

        [Fact]
        public async Task RunCommand_Execute_ServiceThrowsComfyUIException_ShowsDangerSnackbar()
        {
            var fake = new FakeCaptioningService { ThrowOnProcessDirectory = true, ThrowMessage = "接続エラー" };
            var vm = await CreateReadyVmAsync(fake);

            RunOnSta(async () => await vm.RunCommand.ExecuteAsync(null));

            Assert.Contains(_fakeSnackbar.Calls,
                c => c.Appearance == ControlAppearance.Danger && c.Message == "接続エラー");
        }

        [Fact]
        public async Task RunCommand_Execute_ServiceThrowsComfyUIException_ResetsIsRunning()
        {
            var fake = new FakeCaptioningService { ThrowOnProcessDirectory = true };
            var vm = await CreateReadyVmAsync(fake);

            RunOnSta(async () => await vm.RunCommand.ExecuteAsync(null));

            Assert.False(vm.IsRunning);
        }
    }
}
