using System.IO;
using ComfyUICaptioningTool.Helpers;
using ComfyUICaptioningTool.Models;
using ComfyUICaptioningTool.ViewModels.Pages;
using ComfyUILibs.Common;

namespace ComfyUICaptioningToolTests.ViewModels.Pages
{
    public class DataViewModelTests : IDisposable
    {
        private readonly string _tempDir;

        public DataViewModelTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose() => Directory.Delete(_tempDir, recursive: true);

        private Setting<AppConfig> CreateSetting(string? resultsFolder = null)
        {
            var setting = new Setting<AppConfig>(Path.Combine(_tempDir, "setting.json"), onLoad: false);
            setting.Data.ResultsFolder = resultsFolder ?? Path.Combine(_tempDir, "Results");
            return setting;
        }

        /// <summary>captioning_result_*.json と同じ形式の JSON ファイルをテスト用に書き出す。</summary>
        private static void WriteResultFile(
            string resultsFolder, string fileName, string status,
            int processed = 0, int skipped = 0, int errors = 0, string? error = null)
        {
            Directory.CreateDirectory(resultsFolder);
            var errorJson = error is null ? "null" : $"\"{error}\"";
            var json = $$"""
                {
                  "status": "{{status}}",
                  "timestamp": "2026-07-14T10:00:00",
                  "directory": "C:\\images",
                  "recursive": false,
                  "processed": {{processed}},
                  "skipped": {{skipped}},
                  "errors": {{errors}},
                  "error": {{errorJson}},
                  "log_entries": ["[1/1] a.jpg → OK"],
                  "config": null
                }
                """;
            File.WriteAllText(Path.Combine(resultsFolder, fileName), json);
        }

        // ── コンストラクター ───────────────────────────────────────────────────

        [Fact]
        public void Constructor_Config_IsSet()
        {
            var setting = CreateSetting();

            var vm = new DataViewModel(setting);

            Assert.Same(setting, vm.Config);
        }

        // ── OnNavigatedToAsync ────────────────────────────────────────────────

        [Fact]
        public async Task OnNavigatedToAsync_ResultsFolderNotSet_ShowsStatusMessage()
        {
            var setting = CreateSetting(resultsFolder: "");
            var vm = new DataViewModel(setting);

            await vm.OnNavigatedToAsync();

            Assert.Empty(vm.Results);
            Assert.Equal(LocalizationManager.Instance["Data_ResultsFolderNotSet"], vm.StatusMessage);
        }

        [Fact]
        public async Task OnNavigatedToAsync_ResultsFolderNotFound_ShowsStatusMessage()
        {
            var folder = Path.Combine(_tempDir, "NoSuchFolder");
            var vm = new DataViewModel(CreateSetting(folder));

            await vm.OnNavigatedToAsync();

            Assert.Empty(vm.Results);
            Assert.Equal(
                string.Format(LocalizationManager.Instance["Data_FolderNotFound_Format"], folder),
                vm.StatusMessage);
        }

        [Fact]
        public async Task OnNavigatedToAsync_EmptyFolder_ShowsNoResultsMessage()
        {
            var folder = Path.Combine(_tempDir, "Results");
            Directory.CreateDirectory(folder);
            var vm = new DataViewModel(CreateSetting(folder));

            await vm.OnNavigatedToAsync();

            Assert.Empty(vm.Results);
            Assert.Equal(LocalizationManager.Instance["Data_NoResults"], vm.StatusMessage);
        }

        [Fact]
        public async Task OnNavigatedToAsync_LoadsResultFiles_NewestFirst()
        {
            var folder = Path.Combine(_tempDir, "Results");
            WriteResultFile(folder, "captioning_result_20260714_090000.json", "success", processed: 3, skipped: 1, errors: 0);
            WriteResultFile(folder, "captioning_result_20260714_100000.json", "error", error: "ComfyUI に接続できません");
            var vm = new DataViewModel(CreateSetting(folder));

            await vm.OnNavigatedToAsync();

            Assert.Equal(2, vm.Results.Count);
            Assert.Equal("", vm.StatusMessage);
            Assert.False(vm.Results[0].IsSuccess);
            Assert.Contains("ComfyUI に接続できません", vm.Results[0].SummaryText);
            Assert.True(vm.Results[1].IsSuccess);
            Assert.Equal(
                string.Format(LocalizationManager.Instance["Main_SummaryFormat"], 3, 1, 0),
                vm.Results[1].SummaryText);
        }

        [Fact]
        public async Task OnNavigatedToAsync_MalformedFile_IsSkipped()
        {
            var folder = Path.Combine(_tempDir, "Results");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "captioning_result_broken.json"), "{ not json");
            var vm = new DataViewModel(CreateSetting(folder));

            await vm.OnNavigatedToAsync();

            Assert.Empty(vm.Results);
            Assert.Equal(LocalizationManager.Instance["Data_NoResults"], vm.StatusMessage);
        }

        // ── OnNavigatedFromAsync ──────────────────────────────────────────────

        [Fact]
        public async Task OnNavigatedFromAsync_ReturnsCompletedTask()
        {
            var vm = new DataViewModel(CreateSetting());

            var task = vm.OnNavigatedFromAsync();
            await task;

            Assert.True(task.IsCompletedSuccessfully);
        }

        // ── RefreshCommand ────────────────────────────────────────────────────

        [Fact]
        public async Task RefreshCommand_Execute_ReloadsResults()
        {
            var folder = Path.Combine(_tempDir, "Results");
            var vm = new DataViewModel(CreateSetting(folder));
            await vm.OnNavigatedToAsync();
            Assert.Empty(vm.Results);

            WriteResultFile(folder, "captioning_result_20260714_090000.json", "success", processed: 1);
            await vm.RefreshCommand.ExecuteAsync(null);

            Assert.Single(vm.Results);
        }
    }
}
