using System.IO;
using ComfyUICaptioningTool.Models;
using ComfyUICaptioningTool.Services;
using ComfyUICaptioningToolTests.Fakes;
using ComfyUILibs.Services;

namespace ComfyUICaptioningToolTests.Services
{
    public class TagReportGeneratorTests : IDisposable
    {
        private readonly string _tempDir;

        public TagReportGeneratorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose() => Directory.Delete(_tempDir, recursive: true);

        [Fact]
        public async Task GenerateAsync_CallsServiceWithDirectoryAndRecursive()
        {
            File.WriteAllText(Path.Combine(_tempDir, CaptioningService.ReportFileName), "1girl: 3\n");
            var fake = new FakeCaptioningService();

            await TagReportGenerator.GenerateAsync(fake, _tempDir, recursive: true);

            Assert.True(fake.GenerateReportCalled);
            Assert.Equal(_tempDir, fake.GenerateReportArgDirectory);
            Assert.True(fake.GenerateReportArgRecursive);
        }

        [Fact]
        public async Task GenerateAsync_ParsesReportLinesIntoEntries()
        {
            var reportPath = Path.Combine(_tempDir, CaptioningService.ReportFileName);
            File.WriteAllLines(reportPath, new[] { "1girl: 3", "solo: 2", "blue eyes: 1" });
            var fake = new FakeCaptioningService();

            var entries = await TagReportGenerator.GenerateAsync(fake, _tempDir, recursive: false);

            Assert.Equal(3, entries.Count);
            Assert.Equal(new TagCountEntry("1girl", 3), entries[0]);
            Assert.Equal(new TagCountEntry("solo", 2), entries[1]);
            Assert.Equal(new TagCountEntry("blue eyes", 1), entries[2]);
        }

        [Fact]
        public async Task GenerateAsync_TagNameContainingColon_ParsedCorrectly()
        {
            var reportPath = Path.Combine(_tempDir, CaptioningService.ReportFileName);
            File.WriteAllLines(reportPath, new[] { "rating:general: 5" });
            var fake = new FakeCaptioningService();

            var entries = await TagReportGenerator.GenerateAsync(fake, _tempDir, recursive: false);

            Assert.Equal(new TagCountEntry("rating:general", 5), Assert.Single(entries));
        }

        [Fact]
        public async Task GenerateAsync_ServiceThrows_PropagatesException()
        {
            var fake = new FakeCaptioningService { ThrowOnGenerateReport = true, ThrowMessage = "書き込み失敗" };

            var ex = await Assert.ThrowsAsync<ComfyUILibs.Exceptions.ComfyUIException>(
                () => TagReportGenerator.GenerateAsync(fake, _tempDir, recursive: false));

            Assert.Equal("書き込み失敗", ex.Message);
        }
    }
}
