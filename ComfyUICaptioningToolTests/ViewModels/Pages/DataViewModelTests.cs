using System.IO;
using ComfyUICaptioningTool.Services;
using ComfyUICaptioningTool.ViewModels.Pages;

namespace ComfyUICaptioningToolTests.ViewModels.Pages
{
    public class DataViewModelTests
    {
        private readonly CaptioningRunResultStore _resultStore = new();

        private DataViewModel CreateVm() => new DataViewModel(_resultStore);

        // ── コンストラクター ───────────────────────────────────────────────────

        [Fact]
        public void Constructor_ResultStore_IsSet()
        {
            var vm = CreateVm();

            Assert.Same(_resultStore, vm.ResultStore);
        }

        // ── 直近の実行結果 ─────────────────────────────────────────────────────

        [Fact]
        public void HasLastResult_NoResult_IsFalse()
        {
            var vm = CreateVm();

            Assert.False(vm.HasLastResult);
        }

        [Fact]
        public void LastResultSummary_NoResult_ReturnsNoResultMessage()
        {
            var vm = CreateVm();

            Assert.Equal(
                ComfyUICaptioningTool.Helpers.LocalizationManager.Instance["Data_NoResultYet"],
                vm.LastResultSummary);
        }

        [Fact]
        public void HasLastResult_AfterResultStoreUpdated_IsTrue()
        {
            var vm = CreateVm();
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            _resultStore.LastResult = new ComfyUICaptioningTool.Models.CaptioningRunResult(
                DateTime.Now, tempDir, false, 3, 1, 0, new List<string> { "[1/4] a.jpg → OK" });

            Assert.True(vm.HasLastResult);
            Assert.Equal(tempDir, vm.LastResultDirectory);
            Assert.Single(vm.LastResultLogEntries);
        }
    }
}
