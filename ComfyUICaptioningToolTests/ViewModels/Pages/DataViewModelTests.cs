using ComfyUICaptioningTool.ViewModels.Pages;

namespace ComfyUICaptioningToolTests.ViewModels.Pages
{
    public class DataViewModelTests
    {
        // ── OnNavigatedToAsync ────────────────────────────────────────────────

        [Fact]
        public async Task OnNavigatedToAsync_PopulatesColors_With8192Items()
        {
            var vm = new DataViewModel();

            await vm.OnNavigatedToAsync();

            Assert.Equal(8192, vm.Colors.Count());
        }

        [Fact]
        public async Task OnNavigatedToAsync_CalledTwice_DoesNotRegenerateColors()
        {
            var vm = new DataViewModel();
            await vm.OnNavigatedToAsync();
            var first = vm.Colors;

            await vm.OnNavigatedToAsync();

            Assert.Same(first, vm.Colors);
        }

        // ── OnNavigatedFromAsync ──────────────────────────────────────────────

        [Fact]
        public async Task OnNavigatedFromAsync_ReturnsCompletedTask()
        {
            var vm = new DataViewModel();

            var task = vm.OnNavigatedFromAsync();
            await task;

            Assert.True(task.IsCompletedSuccessfully);
        }
    }
}
