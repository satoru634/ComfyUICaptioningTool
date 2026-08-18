using System.IO;
using System.Runtime.ExceptionServices;
using ComfyUICaptioningTool.Models;
using ComfyUICaptioningTool.ViewModels.Pages;
using ComfyUICaptioningToolTests.Fakes;
using ComfyUILibs.Common;
using Wpf.Ui.Controls;

namespace ComfyUICaptioningToolTests.ViewModels.Pages
{
    public class ConfigViewModelTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly FakeSnackbarService _fakeSnackbar;

        public ConfigViewModelTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
            _fakeSnackbar = new FakeSnackbarService();
        }

        public void Dispose() => Directory.Delete(_tempDir, recursive: true);

        private string ConfigPath => Path.Combine(_tempDir, "captioning_config.json");

        private Setting<AppConfig> CreateSetting(string? configPath = null)
        {
            var setting = new Setting<AppConfig>(Path.Combine(_tempDir, "setting.json"), onLoad: false);
            setting.Data.ConfigPath = configPath ?? ConfigPath;
            return setting;
        }

        private ConfigViewModel CreateVm(Setting<AppConfig>? setting = null)
            => new ConfigViewModel(setting ?? CreateSetting(), _fakeSnackbar);

        private void WriteConfigFile(string json) => File.WriteAllText(ConfigPath, json);

        /// <summary>
        /// スナックバー表示（SymbolIcon 生成）を伴う処理は STA スレッドが必要なため、
        /// MainWindowViewModelTests.RunOnSta と同じパターンでラップする。
        /// </summary>
        private static void RunOnSta(Action action)
        {
            lock (ComfyUICaptioningToolTests.TestSupport.StaThreadGate.Lock)
            {
                Exception? caught = null;
                var thread = new Thread(() =>
                {
                    try { action(); }
                    catch (Exception ex) { caught = ex; }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
                if (caught is not null)
                    ExceptionDispatchInfo.Capture(caught).Throw();
            }
        }

        // ── コンストラクター ───────────────────────────────────────────────────

        [Fact]
        public void Constructor_Config_IsSet()
        {
            var setting = CreateSetting();

            var vm = CreateVm(setting);

            Assert.Same(setting, vm.Config);
        }

        // ── ModelNameList ─────────────────────────────────────────────────────

        [Fact]
        public void ModelNameList_ContainsAllFiveWd14Models()
        {
            var vm = CreateVm();

            Assert.Equal(5, vm.ModelNameList.Count);
            Assert.Contains("wd-vit-tagger-v3", vm.ModelNameList);
            Assert.Contains("wd-swinv2-tagger-v3", vm.ModelNameList);
            Assert.Contains("wd-convnext-tagger-v3", vm.ModelNameList);
            Assert.Contains("wd-eva02-large-tagger-v3", vm.ModelNameList);
            Assert.Contains("wd-vit-large-tagger-v3", vm.ModelNameList);
        }

        // ── OnNavigatedToAsync / LoadFromFile ─────────────────────────────────

        [Fact]
        public void OnNavigatedToAsync_EmptyConfigPath_SetsIsConfigLoadedFalse()
        {
            var setting = CreateSetting();
            setting.Data.ConfigPath = "";
            var vm = CreateVm(setting);

            RunOnSta(() => vm.OnNavigatedToAsync().GetAwaiter().GetResult());

            Assert.False(vm.IsConfigLoaded);
        }

        [Fact]
        public void OnNavigatedToAsync_EmptyConfigPath_ShowsDangerSnackbar()
        {
            var setting = CreateSetting();
            setting.Data.ConfigPath = "";
            var vm = CreateVm(setting);

            RunOnSta(() => vm.OnNavigatedToAsync().GetAwaiter().GetResult());

            Assert.Contains(_fakeSnackbar.Calls, c => c.Appearance == ControlAppearance.Danger);
        }

        [Fact]
        public async Task OnNavigatedToAsync_FileNotFound_SetsIsConfigLoadedTrue()
        {
            var vm = CreateVm();

            await vm.OnNavigatedToAsync();

            Assert.True(vm.IsConfigLoaded);
        }

        [Fact]
        public async Task OnNavigatedToAsync_FileNotFound_UsesDefaultThresholds()
        {
            var vm = CreateVm();

            await vm.OnNavigatedToAsync();

            Assert.Equal(0.35, vm.GeneralThreshold);
            Assert.Equal(0.85, vm.CharacterThreshold);
        }

        [Fact]
        public async Task OnNavigatedToAsync_FileNotFound_SetsNewFileNoticeInStatusText()
        {
            var vm = CreateVm();

            await vm.OnNavigatedToAsync();

            Assert.NotEqual("", vm.StatusText);
        }

        [Fact]
        public async Task OnNavigatedToAsync_ValidConfig_PopulatesFields()
        {
            WriteConfigFile("""
                {
                  "comfyui_url": "http://127.0.0.1:8188",
                  "wd14_tagger": {
                    "model_name": "wd-eva02-large-tagger-v3",
                    "general_threshold": 0.4,
                    "character_threshold": 0.9
                  },
                  "prepend_tags": ["my_chara"],
                  "exclude_tags": ["rating:general"]
                }
                """);
            var vm = CreateVm();

            await vm.OnNavigatedToAsync();

            Assert.True(vm.IsConfigLoaded);
            Assert.Equal("http://127.0.0.1:8188", vm.ComfyUiUrl);
            Assert.Equal("wd-eva02-large-tagger-v3", vm.ModelName);
            Assert.Equal(0.4, vm.GeneralThreshold);
            Assert.Equal(0.9, vm.CharacterThreshold);
            Assert.Equal("my_chara", vm.PrependTagsText);
            Assert.Equal("rating:general", vm.ExcludeTagsText);
        }

        [Fact]
        public void OnNavigatedToAsync_InvalidJson_SetsIsConfigLoadedFalse()
        {
            WriteConfigFile("{ invalid json");
            var vm = CreateVm();

            RunOnSta(() => vm.OnNavigatedToAsync().GetAwaiter().GetResult());

            Assert.False(vm.IsConfigLoaded);
        }

        [Fact]
        public void OnNavigatedToAsync_InvalidJson_ShowsDangerSnackbar()
        {
            WriteConfigFile("{ invalid json");
            var vm = CreateVm();

            RunOnSta(() => vm.OnNavigatedToAsync().GetAwaiter().GetResult());

            Assert.Contains(_fakeSnackbar.Calls, c => c.Appearance == ControlAppearance.Danger);
        }

        [Fact]
        public async Task OnNavigatedFromAsync_ReturnsCompletedTask()
        {
            var vm = CreateVm();

            var task = vm.OnNavigatedFromAsync();
            await task;

            Assert.True(task.IsCompletedSuccessfully);
        }

        // ── SaveCommand CanExecute ────────────────────────────────────────────

        [Fact]
        public void SaveCommand_CanExecute_FalseBeforeLoad()
        {
            var vm = CreateVm();

            Assert.False(vm.SaveCommand.CanExecute(null));
        }

        [Fact]
        public async Task SaveCommand_CanExecute_TrueAfterLoad()
        {
            var vm = CreateVm();

            await vm.OnNavigatedToAsync();

            Assert.True(vm.SaveCommand.CanExecute(null));
        }

        // ── SaveCommand 実行 ───────────────────────────────────────────────────

        [Fact]
        public async Task SaveCommand_Execute_WritesFieldsToFile()
        {
            var vm = CreateVm();
            await vm.OnNavigatedToAsync();
            vm.ComfyUiUrl = "http://127.0.0.1:8188";
            vm.ModelName = "wd-eva02-large-tagger-v3";
            vm.GeneralThreshold = 0.4;
            vm.CharacterThreshold = 0.9;
            vm.PrependTagsText = "my_chara, 1girl";
            vm.ExcludeTagsText = "rating:general";

            RunOnSta(() => vm.SaveCommand.Execute(null));

            var json = File.ReadAllText(ConfigPath);
            Assert.Contains("\"comfyui_url\"", json);
            Assert.Contains("wd-eva02-large-tagger-v3", json);
            Assert.Contains("my_chara", json);
            Assert.Contains("1girl", json);
            Assert.Contains("rating:general", json);
            Assert.DoesNotContain("default_workflow", json);
            Assert.DoesNotContain("workflows", json);
        }

        [Fact]
        public async Task SaveCommand_Execute_Success_ShowsSuccessSnackbar()
        {
            var vm = CreateVm();
            await vm.OnNavigatedToAsync();
            vm.ComfyUiUrl = "http://127.0.0.1:8188";
            vm.ModelName = "wd-eva02-large-tagger-v3";

            RunOnSta(() => vm.SaveCommand.Execute(null));

            Assert.Contains(_fakeSnackbar.Calls, c => c.Appearance == ControlAppearance.Success);
        }

        [Fact]
        public async Task SaveCommand_Execute_EmptyComfyUiUrl_ShowsDangerSnackbar_DoesNotWriteFile()
        {
            var vm = CreateVm();
            await vm.OnNavigatedToAsync();
            vm.ComfyUiUrl = "";
            vm.ModelName = "wd-eva02-large-tagger-v3";

            RunOnSta(() => vm.SaveCommand.Execute(null));

            Assert.Contains(_fakeSnackbar.Calls, c => c.Appearance == ControlAppearance.Danger);
            Assert.False(File.Exists(ConfigPath));
        }

        [Fact]
        public async Task SaveCommand_Execute_EmptyModelName_ShowsDangerSnackbar_DoesNotWriteFile()
        {
            var vm = CreateVm();
            await vm.OnNavigatedToAsync();
            vm.ComfyUiUrl = "http://127.0.0.1:8188";
            vm.ModelName = "";

            RunOnSta(() => vm.SaveCommand.Execute(null));

            Assert.Contains(_fakeSnackbar.Calls, c => c.Appearance == ControlAppearance.Danger);
            Assert.False(File.Exists(ConfigPath));
        }

        [Fact]
        public async Task SaveCommand_Execute_ThresholdOutOfRange_ShowsDangerSnackbar_DoesNotWriteFile()
        {
            var vm = CreateVm();
            await vm.OnNavigatedToAsync();
            vm.ComfyUiUrl = "http://127.0.0.1:8188";
            vm.ModelName = "wd-eva02-large-tagger-v3";
            vm.GeneralThreshold = 1.5;

            RunOnSta(() => vm.SaveCommand.Execute(null));

            Assert.Contains(_fakeSnackbar.Calls, c => c.Appearance == ControlAppearance.Danger);
            Assert.False(File.Exists(ConfigPath));
        }

        [Fact]
        public async Task SaveCommand_Execute_BlankTagsText_WritesEmptyTagLists()
        {
            var vm = CreateVm();
            await vm.OnNavigatedToAsync();
            vm.ComfyUiUrl = "http://127.0.0.1:8188";
            vm.ModelName = "wd-eva02-large-tagger-v3";
            vm.PrependTagsText = "";
            vm.ExcludeTagsText = "";

            RunOnSta(() => vm.SaveCommand.Execute(null));

            var json = File.ReadAllText(ConfigPath);
            Assert.Contains("\"prepend_tags\": []", json);
            Assert.Contains("\"exclude_tags\": []", json);
        }

        // ── SaveCommand 実行（TaggerBackend.WdV3Timm） ─────────────────────────
        // wdv3-timm はモデル名・しきい値・実行ファイルパスを独自に持たず wd14_tagger（ModelName/
        // GeneralThreshold/CharacterThreshold）を共用し、実行ファイルは WdV3TimmPaths の固定パスを使う
        // （captioning_config.json では扱わない）ため、Save() は wd14_tagger セクションのみを検証する。

        [Fact]
        public async Task SaveCommand_Execute_WdV3TimmBackend_ValidModelName_WritesFile_DoesNotRequireComfyUiUrl()
        {
            var setting = CreateSetting();
            setting.Data.TaggerBackend = TaggerBackend.WdV3Timm;
            var vm = CreateVm(setting);
            await vm.OnNavigatedToAsync();
            vm.ComfyUiUrl = "";
            vm.ModelName = "wd-vit-tagger-v3";

            RunOnSta(() => vm.SaveCommand.Execute(null));

            Assert.Contains(_fakeSnackbar.Calls, c => c.Appearance == ControlAppearance.Success);
            var json = File.ReadAllText(ConfigPath);
            Assert.DoesNotContain("comfyui_url", json);
            Assert.Contains("wd14_tagger", json);
        }

        [Fact]
        public async Task SaveCommand_Execute_WdV3TimmBackend_MissingModelName_ShowsDangerSnackbar_DoesNotWriteFile()
        {
            var setting = CreateSetting();
            setting.Data.TaggerBackend = TaggerBackend.WdV3Timm;
            var vm = CreateVm(setting);
            await vm.OnNavigatedToAsync();
            vm.ModelName = "";

            RunOnSta(() => vm.SaveCommand.Execute(null));

            Assert.Contains(_fakeSnackbar.Calls, c => c.Appearance == ControlAppearance.Danger);
            Assert.False(File.Exists(ConfigPath));
        }

        [Fact]
        public async Task SaveCommand_Execute_WdV3TimmBackend_UnmappedModelName_ShowsDangerSnackbar_DoesNotWriteFile()
        {
            var setting = CreateSetting();
            setting.Data.TaggerBackend = TaggerBackend.WdV3Timm;
            var vm = CreateVm(setting);
            await vm.OnNavigatedToAsync();
            vm.ModelName = "wd-v1-4-moat-tagger-v2";

            RunOnSta(() => vm.SaveCommand.Execute(null));

            Assert.Contains(_fakeSnackbar.Calls, c => c.Appearance == ControlAppearance.Danger);
            Assert.False(File.Exists(ConfigPath));
        }

        [Fact]
        public async Task SaveCommand_Execute_WdV3TimmBackend_ThresholdOutOfRange_ShowsDangerSnackbar_DoesNotWriteFile()
        {
            var setting = CreateSetting();
            setting.Data.TaggerBackend = TaggerBackend.WdV3Timm;
            var vm = CreateVm(setting);
            await vm.OnNavigatedToAsync();
            vm.ModelName = "wd-vit-tagger-v3";
            vm.GeneralThreshold = 1.5;

            RunOnSta(() => vm.SaveCommand.Execute(null));

            Assert.Contains(_fakeSnackbar.Calls, c => c.Appearance == ControlAppearance.Danger);
            Assert.False(File.Exists(ConfigPath));
        }
    }
}
