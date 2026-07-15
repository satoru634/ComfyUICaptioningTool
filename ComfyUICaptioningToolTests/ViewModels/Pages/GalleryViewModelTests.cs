using System.IO;
using ComfyUICaptioningTool.Helpers;
using ComfyUICaptioningTool.Models;
using ComfyUICaptioningTool.ViewModels.Pages;
using ComfyUILibs.Common;

namespace ComfyUICaptioningToolTests.ViewModels.Pages
{
    public class GalleryViewModelTests : IDisposable
    {
        private readonly string _tempDir;

        public GalleryViewModelTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose() => Directory.Delete(_tempDir, recursive: true);

        private Setting<AppConfig> CreateSetting()
            => new(Path.Combine(_tempDir, "setting.json"), onLoad: false);

        // ── コンストラクター ───────────────────────────────────────────────────

        [Fact]
        public void Constructor_Config_IsSet()
        {
            var setting = CreateSetting();

            var vm = new GalleryViewModel(setting);

            Assert.Same(setting, vm.Config);
        }

        [Fact]
        public void Constructor_InitialState_IsEmpty()
        {
            var vm = new GalleryViewModel(CreateSetting());

            Assert.Empty(vm.Images);
            Assert.Equal("", vm.StatusMessage);
            Assert.False(vm.IsLoading);
        }

        // ── LoadCommand.CanExecute ────────────────────────────────────────────

        [Fact]
        public void LoadCommand_CanExecute_False_WhenDirectoryNotSet()
        {
            var vm = new GalleryViewModel(CreateSetting());

            Assert.False(vm.LoadCommand.CanExecute(null));
        }

        [Fact]
        public void LoadCommand_CanExecute_True_WhenDirectorySet()
        {
            var vm = new GalleryViewModel(CreateSetting()) { TargetDirectory = _tempDir };

            Assert.True(vm.LoadCommand.CanExecute(null));
        }

        // ── LoadCommand 実行 ──────────────────────────────────────────────────

        [Fact]
        public async Task LoadCommand_Execute_DirectoryNotFound_ShowsStatusMessage()
        {
            var directory = Path.Combine(_tempDir, "NoSuchFolder");
            var vm = new GalleryViewModel(CreateSetting()) { TargetDirectory = directory };

            await vm.LoadCommand.ExecuteAsync(null);

            Assert.Empty(vm.Images);
            Assert.Equal(
                string.Format(LocalizationManager.Instance["Gallery_FolderNotFound_Format"], directory),
                vm.StatusMessage);
        }

        [Fact]
        public async Task LoadCommand_Execute_NoImages_ShowsNoImagesMessage()
        {
            var vm = new GalleryViewModel(CreateSetting()) { TargetDirectory = _tempDir };

            await vm.LoadCommand.ExecuteAsync(null);

            Assert.Empty(vm.Images);
            Assert.Equal(LocalizationManager.Instance["Gallery_NoImages"], vm.StatusMessage);
        }

        [Fact]
        public async Task LoadCommand_Execute_ImageWithTagsFile_LoadsTrimmedNonEmptyTags()
        {
            File.WriteAllBytes(Path.Combine(_tempDir, "a.jpg"), new byte[] { 1, 2, 3 });
            File.WriteAllText(Path.Combine(_tempDir, "a.txt"), "tag_a, tag_b ,  , tag_a");
            var vm = new GalleryViewModel(CreateSetting()) { TargetDirectory = _tempDir };

            await vm.LoadCommand.ExecuteAsync(null);

            var entry = Assert.Single(vm.Images);
            Assert.Equal("a.jpg", entry.FileName);
            Assert.True(entry.HasTags);
            Assert.Equal(new[] { "tag_a", "tag_b", "tag_a" }, entry.Tags);
        }

        [Fact]
        public async Task LoadCommand_Execute_ImageWithoutTagsFile_HasNoTags()
        {
            File.WriteAllBytes(Path.Combine(_tempDir, "b.png"), new byte[] { 1, 2, 3 });
            var vm = new GalleryViewModel(CreateSetting()) { TargetDirectory = _tempDir };

            await vm.LoadCommand.ExecuteAsync(null);

            var entry = Assert.Single(vm.Images);
            Assert.False(entry.HasTags);
            Assert.Empty(entry.Tags);
        }

        [Fact]
        public async Task LoadCommand_Execute_UnsupportedExtension_IsExcluded()
        {
            File.WriteAllText(Path.Combine(_tempDir, "note.txt"), "not an image");
            File.WriteAllBytes(Path.Combine(_tempDir, "c.gif"), new byte[] { 1, 2, 3 });
            var vm = new GalleryViewModel(CreateSetting()) { TargetDirectory = _tempDir };

            await vm.LoadCommand.ExecuteAsync(null);

            Assert.Empty(vm.Images);
        }

        [Fact]
        public async Task LoadCommand_Execute_Recursive_False_ExcludesSubdirectoryImages()
        {
            var subDir = Path.Combine(_tempDir, "sub");
            Directory.CreateDirectory(subDir);
            File.WriteAllBytes(Path.Combine(_tempDir, "top.jpg"), new byte[] { 1 });
            File.WriteAllBytes(Path.Combine(subDir, "nested.jpg"), new byte[] { 1 });
            var vm = new GalleryViewModel(CreateSetting()) { TargetDirectory = _tempDir, Recursive = false };

            await vm.LoadCommand.ExecuteAsync(null);

            var entry = Assert.Single(vm.Images);
            Assert.Equal("top.jpg", entry.FileName);
        }

        [Fact]
        public async Task LoadCommand_Execute_Recursive_True_IncludesSubdirectoryImages()
        {
            var subDir = Path.Combine(_tempDir, "sub");
            Directory.CreateDirectory(subDir);
            File.WriteAllBytes(Path.Combine(_tempDir, "top.jpg"), new byte[] { 1 });
            File.WriteAllBytes(Path.Combine(subDir, "nested.jpg"), new byte[] { 1 });
            var vm = new GalleryViewModel(CreateSetting()) { TargetDirectory = _tempDir, Recursive = true };

            await vm.LoadCommand.ExecuteAsync(null);

            Assert.Equal(2, vm.Images.Count);
        }

        [Fact]
        public async Task LoadCommand_Execute_SortsByFileNameAscending()
        {
            File.WriteAllBytes(Path.Combine(_tempDir, "b.jpg"), new byte[] { 1 });
            File.WriteAllBytes(Path.Combine(_tempDir, "a.jpg"), new byte[] { 1 });
            var vm = new GalleryViewModel(CreateSetting()) { TargetDirectory = _tempDir };

            await vm.LoadCommand.ExecuteAsync(null);

            Assert.Equal(new[] { "a.jpg", "b.jpg" }, vm.Images.Select(i => i.FileName));
        }

        [Fact]
        public async Task LoadCommand_Execute_InvalidImageBytes_ThumbnailIsNullButEntryIsIncluded()
        {
            File.WriteAllText(Path.Combine(_tempDir, "broken.png"), "this is not a valid png");
            var vm = new GalleryViewModel(CreateSetting()) { TargetDirectory = _tempDir };

            await vm.LoadCommand.ExecuteAsync(null);

            var entry = Assert.Single(vm.Images);
            Assert.Null(entry.Thumbnail);
        }
    }
}
