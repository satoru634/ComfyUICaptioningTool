using System.ComponentModel;
using System.Globalization;
using System.IO;
using ComfyUICaptioningTool.Helpers;
using ComfyUICaptioningTool.Models;
using ComfyUICaptioningTool.Services;
using ComfyUICaptioningTool.ViewModels.Pages;
using ComfyUICaptioningToolTests.Fakes;
using ComfyUILibs.Common;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace ComfyUICaptioningToolTests.ViewModels.Pages
{
    public class SettingsViewModelTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly FakeSnackbarService _fakeSnackbar;

        public SettingsViewModelTests()
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

        private SettingsViewModel CreateVm(
            Setting<AppConfig>? setting = null, IWdV3TimmBuildService? wdV3TimmBuildService = null)
            => new SettingsViewModel(setting ?? CreateSetting(), _fakeSnackbar, wdV3TimmBuildService);

        /// <summary>
        /// スナックバー表示（SymbolIcon 生成）を伴う BuildWdV3TimmCommand 実行は STA スレッドが必要なため、
        /// MainPageViewModelTests.RunOnSta と同じパターンで TestSupport.StaTestRunner に委譲する。
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

        // ── ThemeList ─────────────────────────────────────────────────────────

        [Fact]
        public void ThemeList_Count_IsTwo()
        {
            var vm = CreateVm();

            Assert.Equal(2, vm.ThemeList.Count);
        }

        [Fact]
        public void ThemeList_ContainsLight()
        {
            var vm = CreateVm();

            Assert.Contains(ApplicationTheme.Light, vm.ThemeList);
        }

        [Fact]
        public void ThemeList_ContainsDark()
        {
            var vm = CreateVm();

            Assert.Contains(ApplicationTheme.Dark, vm.ThemeList);
        }

        // ── TaggerBackendList ─────────────────────────────────────────────────

        [Fact]
        public void TaggerBackendList_Count_IsTwo()
        {
            var vm = CreateVm();

            Assert.Equal(2, vm.TaggerBackendList.Count);
        }

        [Fact]
        public void TaggerBackendList_ContainsComfyUI()
        {
            var vm = CreateVm();

            Assert.Contains(TaggerBackend.ComfyUI, vm.TaggerBackendList);
        }

        [Fact]
        public void TaggerBackendList_ContainsWdV3Timm()
        {
            var vm = CreateVm();

            Assert.Contains(TaggerBackend.WdV3Timm, vm.TaggerBackendList);
        }

        // ── SelectedTaggerBackend ─────────────────────────────────────────────

        [Fact]
        public void SelectedTaggerBackend_Set_UpdatesConfigTaggerBackend()
        {
            var setting = CreateSetting();
            var vm = CreateVm(setting);

            vm.SelectedTaggerBackend = TaggerBackend.WdV3Timm;

            Assert.Equal(TaggerBackend.WdV3Timm, setting.Data.TaggerBackend);
        }

        [Fact]
        public void SelectedTaggerBackend_Set_RaisesPropertyChanged()
        {
            var vm = CreateVm();
            var changed = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.SelectedTaggerBackend = TaggerBackend.WdV3Timm;

            Assert.Contains("SelectedTaggerBackend", changed);
        }

        [Fact]
        public async Task OnNavigatedToAsync_SelectedTaggerBackend_LoadedFromConfig()
        {
            var setting = CreateSetting();
            setting.Data.TaggerBackend = TaggerBackend.WdV3Timm;
            var vm = CreateVm(setting);

            await vm.OnNavigatedToAsync();

            Assert.Equal(TaggerBackend.WdV3Timm, vm.SelectedTaggerBackend);
        }

        // ── OnNavigatedToAsync ────────────────────────────────────────────────

        [Fact]
        public async Task OnNavigatedToAsync_AppVersion_StartsWithAppName()
        {
            var vm = CreateVm();

            await vm.OnNavigatedToAsync();

            Assert.StartsWith("ComfyUICaptioningTool - ", vm.AppVersion);
        }

        [Fact]
        public async Task OnNavigatedToAsync_SelectedTheme_LoadedFromConfig()
        {
            var setting = CreateSetting();
            setting.Data.WindowSetting.Theme = ApplicationTheme.Dark;
            var vm = CreateVm(setting);

            await vm.OnNavigatedToAsync();

            Assert.Equal(ApplicationTheme.Dark, vm.SelectedTheme);
        }

        [Fact]
        public async Task OnNavigatedToAsync_CalledTwice_DoesNotReset()
        {
            var setting = CreateSetting();
            var vm = CreateVm(setting);
            await vm.OnNavigatedToAsync();
            var versionAfterFirst = vm.AppVersion;

            await vm.OnNavigatedToAsync();

            Assert.Equal(versionAfterFirst, vm.AppVersion);
        }

        // ── SelectedTheme ──────────────────────────────────────────────────────

        [Fact]
        public void SelectedTheme_Set_UpdatesConfigTheme()
        {
            var setting = CreateSetting();
            var vm = CreateVm(setting);

            vm.SelectedTheme = ApplicationTheme.Dark;

            Assert.Equal(ApplicationTheme.Dark, setting.Data.WindowSetting.Theme);
        }

        [Fact]
        public void SelectedTheme_SetToLight_UpdatesConfigTheme()
        {
            var setting = CreateSetting();
            setting.Data.WindowSetting.Theme = ApplicationTheme.Dark;
            var vm = CreateVm(setting);

            vm.SelectedTheme = ApplicationTheme.Light;

            Assert.Equal(ApplicationTheme.Light, setting.Data.WindowSetting.Theme);
        }

        [Fact]
        public void SelectedTheme_Set_RaisesPropertyChanged()
        {
            var vm = CreateVm();
            var changed = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.SelectedTheme = ApplicationTheme.Dark;

            Assert.Contains("SelectedTheme", changed);
        }

        // ── OnNavigatedFromAsync ──────────────────────────────────────────────

        [Fact]
        public async Task OnNavigatedFromAsync_ReturnsCompletedTask()
        {
            var vm = CreateVm();

            var task = vm.OnNavigatedFromAsync();
            await task;

            Assert.True(task.IsCompletedSuccessfully);
        }

        // ── LanguageList ──────────────────────────────────────────────────────

        [Fact]
        public void LanguageList_Count_IsTwo()
        {
            var vm = CreateVm();

            Assert.Equal(2, vm.LanguageList.Count);
        }

        [Fact]
        public void LanguageList_ContainsJa()
        {
            var vm = CreateVm();

            Assert.Contains(vm.LanguageList, l => l.Key == "ja");
        }

        [Fact]
        public void LanguageList_ContainsEn()
        {
            var vm = CreateVm();

            Assert.Contains(vm.LanguageList, l => l.Key == "en");
        }

        // ── SelectedLanguage ────────────────────────────────────────────────────
        // LocalizationManager はプロセス全体で共有されるシングルトンのため、
        // テスト間で状態が漏れないよう各テストで元のカルチャを保存・復元する。

        [Fact]
        public void SelectedLanguage_Set_UpdatesConfigLanguage()
        {
            var original = LocalizationManager.Instance.CurrentCulture;
            try
            {
                var setting = CreateSetting();
                var vm = CreateVm(setting);

                vm.SelectedLanguage = "en";

                Assert.Equal("en", setting.Data.Language);
            }
            finally
            {
                LocalizationManager.Instance.CurrentCulture = original;
            }
        }

        [Fact]
        public void SelectedLanguage_Set_UpdatesLocalizationManagerCulture()
        {
            var original = LocalizationManager.Instance.CurrentCulture;
            try
            {
                var vm = CreateVm();

                vm.SelectedLanguage = "en";

                Assert.Equal("en", LocalizationManager.Instance.CurrentCulture.TwoLetterISOLanguageName);
            }
            finally
            {
                LocalizationManager.Instance.CurrentCulture = original;
            }
        }

        [Fact]
        public void SelectedLanguage_Set_RaisesPropertyChanged()
        {
            var original = LocalizationManager.Instance.CurrentCulture;
            try
            {
                var vm = CreateVm();
                var changed = new List<string?>();
                ((INotifyPropertyChanged)vm).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

                vm.SelectedLanguage = "en";

                Assert.Contains("SelectedLanguage", changed);
            }
            finally
            {
                LocalizationManager.Instance.CurrentCulture = original;
            }
        }

        [Fact]
        public async Task OnNavigatedToAsync_SelectedLanguage_LoadedFromConfig()
        {
            var original = LocalizationManager.Instance.CurrentCulture;
            try
            {
                var setting = CreateSetting();
                setting.Data.Language = "en";
                var vm = CreateVm(setting);

                await vm.OnNavigatedToAsync();

                Assert.Equal("en", vm.SelectedLanguage);
            }
            finally
            {
                LocalizationManager.Instance.CurrentCulture = original;
            }
        }

        // ── IsWdV3TimmExeReady / OnNavigatedToAsync ────────────────────────────

        [Fact]
        public async Task OnNavigatedToAsync_ExeReady_SetsIsWdV3TimmExeReadyTrue()
        {
            var fakeBuild = new FakeWdV3TimmBuildService { IsExeReady = true };
            var vm = CreateVm(wdV3TimmBuildService: fakeBuild);

            await vm.OnNavigatedToAsync();

            Assert.True(vm.IsWdV3TimmExeReady);
        }

        [Fact]
        public async Task OnNavigatedToAsync_ExeNotReady_SetsIsWdV3TimmExeReadyFalse()
        {
            var fakeBuild = new FakeWdV3TimmBuildService { IsExeReady = false };
            var vm = CreateVm(wdV3TimmBuildService: fakeBuild);

            await vm.OnNavigatedToAsync();

            Assert.False(vm.IsWdV3TimmExeReady);
        }

        [Fact]
        public async Task OnNavigatedToAsync_CalledAgain_RefreshesIsWdV3TimmExeReady()
        {
            // ビルド後にページを再訪すると最新の状態を反映する（初期化は初回のみだが、
            // 準備状態は毎回再確認する設計であることを検証する）
            var fakeBuild = new FakeWdV3TimmBuildService { IsExeReady = false };
            var vm = CreateVm(wdV3TimmBuildService: fakeBuild);
            await vm.OnNavigatedToAsync();
            Assert.False(vm.IsWdV3TimmExeReady);

            fakeBuild.IsExeReady = true;
            await vm.OnNavigatedToAsync();

            Assert.True(vm.IsWdV3TimmExeReady);
        }

        [Fact]
        public void IsWdV3TimmExeReady_Set_UpdatesWdV3TimmStatusText()
        {
            var vm = CreateVm();

            vm.IsWdV3TimmExeReady = true;
            var readyText = vm.WdV3TimmStatusText;
            vm.IsWdV3TimmExeReady = false;
            var notReadyText = vm.WdV3TimmStatusText;

            Assert.NotEqual(readyText, notReadyText);
        }

        // ── BuildWdV3TimmCommand ──────────────────────────────────────────────

        [Fact]
        public void BuildWdV3TimmCommand_CanExecute_TrueInitially()
        {
            var vm = CreateVm();

            Assert.True(vm.BuildWdV3TimmCommand.CanExecute(null));
        }

        [Fact]
        public void BuildWdV3TimmCommand_Execute_CallsBuildServiceOnce()
        {
            var fakeBuild = new FakeWdV3TimmBuildService { BuildResult = true };
            var vm = CreateVm(wdV3TimmBuildService: fakeBuild);

            RunOnSta(async () => await vm.BuildWdV3TimmCommand.ExecuteAsync(null));

            Assert.Equal(1, fakeBuild.BuildAsyncCallCount);
        }

        [Fact]
        public void BuildWdV3TimmCommand_Execute_AppendsOutputLinesToLog()
        {
            var fakeBuild = new FakeWdV3TimmBuildService
            {
                BuildResult = true,
                OutputLinesToReport = new List<string> { "Creating virtual environment...", "Build complete." },
            };
            var vm = CreateVm(wdV3TimmBuildService: fakeBuild);

            RunOnSta(async () => await vm.BuildWdV3TimmCommand.ExecuteAsync(null));

            Assert.Equal(new[] { "Creating virtual environment...", "Build complete." }, vm.WdV3TimmBuildLogEntries);
        }

        [Fact]
        public void BuildWdV3TimmCommand_Execute_ClearsPreviousLogBeforeAppending()
        {
            var fakeBuild = new FakeWdV3TimmBuildService
            {
                BuildResult = true,
                OutputLinesToReport = new List<string> { "second run line" },
            };
            var vm = CreateVm(wdV3TimmBuildService: fakeBuild);
            vm.WdV3TimmBuildLogEntries.Add("stale line from before");

            RunOnSta(async () => await vm.BuildWdV3TimmCommand.ExecuteAsync(null));

            Assert.Equal(new[] { "second run line" }, vm.WdV3TimmBuildLogEntries);
        }

        [Fact]
        public void BuildWdV3TimmCommand_Execute_TogglesIsBuildingWdV3Timm_BackToFalseAfterCompletion()
        {
            var fakeBuild = new FakeWdV3TimmBuildService { BuildResult = true };
            var vm = CreateVm(wdV3TimmBuildService: fakeBuild);

            RunOnSta(async () => await vm.BuildWdV3TimmCommand.ExecuteAsync(null));

            Assert.False(vm.IsBuildingWdV3Timm);
        }

        [Fact]
        public void BuildWdV3TimmCommand_Execute_Success_RefreshesIsWdV3TimmExeReady()
        {
            var fakeBuild = new FakeWdV3TimmBuildService { BuildResult = true, IsExeReady = false };
            var vm = CreateVm(wdV3TimmBuildService: fakeBuild);
            // BuildAsync が成功した後に IsExeReady を再確認する。フェイクは呼び出し前後で
            // IsExeReady の値を変えないため、ここでは true に切り替えてから実行することで
            // 「BuildAsync 完了後に IsWdV3TimmExeReady が更新される」ことを検証する。
            fakeBuild.IsExeReady = true;

            RunOnSta(async () => await vm.BuildWdV3TimmCommand.ExecuteAsync(null));

            Assert.True(vm.IsWdV3TimmExeReady);
        }

        [Fact]
        public void BuildWdV3TimmCommand_Execute_Success_ShowsSuccessSnackbar()
        {
            var fakeBuild = new FakeWdV3TimmBuildService { BuildResult = true };
            var vm = CreateVm(wdV3TimmBuildService: fakeBuild);

            RunOnSta(async () => await vm.BuildWdV3TimmCommand.ExecuteAsync(null));

            Assert.Contains(_fakeSnackbar.Calls, c => c.Appearance == ControlAppearance.Success);
        }

        [Fact]
        public void BuildWdV3TimmCommand_Execute_Failure_ShowsDangerSnackbar()
        {
            var fakeBuild = new FakeWdV3TimmBuildService { BuildResult = false };
            var vm = CreateVm(wdV3TimmBuildService: fakeBuild);

            RunOnSta(async () => await vm.BuildWdV3TimmCommand.ExecuteAsync(null));

            Assert.Contains(_fakeSnackbar.Calls, c => c.Appearance == ControlAppearance.Danger);
        }

        [Fact]
        public void BuildWdV3TimmCommand_Execute_Failure_TogglesIsBuildingWdV3Timm_BackToFalse()
        {
            var fakeBuild = new FakeWdV3TimmBuildService { BuildResult = false };
            var vm = CreateVm(wdV3TimmBuildService: fakeBuild);

            RunOnSta(async () => await vm.BuildWdV3TimmCommand.ExecuteAsync(null));

            Assert.False(vm.IsBuildingWdV3Timm);
        }
    }
}
