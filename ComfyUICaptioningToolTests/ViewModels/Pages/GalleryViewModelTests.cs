using System.IO;
using ComfyUICaptioningTool.Helpers;
using ComfyUICaptioningTool.Models;
using ComfyUICaptioningTool.Services;
using ComfyUICaptioningTool.ViewModels.Pages;
using ComfyUICaptioningToolTests.Fakes;
using ComfyUILibs.Common;
using ComfyUILibs.Services;

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

        /// <summary>
        /// Wd14TaggerRunner は AppDomain.CurrentDomain.BaseDirectory/templates を参照するため、
        /// テスト実行ディレクトリにテンプレートファイルを配置しておく（ReportViewModelTests と同じ回避策）。
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

        private string WriteValidConfigFile()
        {
            EnsureTemplateFile();
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

        /// <summary>有効な ConfigPath・FakeCaptioningService を設定した ViewModel を作成し、OnNavigatedToAsync まで済ませる。</summary>
        private async Task<GalleryViewModel> CreateReadyVmAsync(FakeCaptioningService fake)
        {
            var setting = CreateSetting();
            setting.Data.ConfigPath = WriteValidConfigFile();
            var vm = new GalleryViewModel(
                setting,
                (Func<Wd14TaggerRunner, IReadOnlyList<string>, IReadOnlyList<string>, ICaptioningService>)((_, _, _) => fake));
            await vm.OnNavigatedToAsync();
            return vm;
        }

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
            Assert.Null(vm.SelectedImage);
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

        // ── SelectedImage（右ペインのタグ一覧表示対象） ───────────────────────────

        [Fact]
        public async Task LoadCommand_Execute_Reload_ResetsSelectedImageToNull()
        {
            File.WriteAllBytes(Path.Combine(_tempDir, "a.jpg"), new byte[] { 1 });
            var vm = new GalleryViewModel(CreateSetting()) { TargetDirectory = _tempDir };
            await vm.LoadCommand.ExecuteAsync(null);
            vm.SelectedImage = vm.Images.Single();

            await vm.LoadCommand.ExecuteAsync(null);

            Assert.Null(vm.SelectedImage);
        }

        [Fact]
        public async Task SelectImageCommand_Execute_SetsSelectedImage()
        {
            File.WriteAllBytes(Path.Combine(_tempDir, "a.jpg"), new byte[] { 1 });
            File.WriteAllBytes(Path.Combine(_tempDir, "b.jpg"), new byte[] { 1 });
            var vm = new GalleryViewModel(CreateSetting()) { TargetDirectory = _tempDir };
            await vm.LoadCommand.ExecuteAsync(null);
            var entryB = vm.Images.Single(i => i.FileName == "b.jpg");

            vm.SelectImageCommand.Execute(entryB);

            Assert.Same(entryB, vm.SelectedImage);
        }

        // ── 一括タグ操作 ──────────────────────────────────────────────────────

        [Fact]
        public void BulkAddTagCommand_CanExecute_False_WhenImagesEmpty()
        {
            var vm = new GalleryViewModel(CreateSetting()) { BulkTagInput = "tag" };

            Assert.False(vm.BulkAddTagCommand.CanExecute(null));
            Assert.False(vm.BulkAddTagToStartCommand.CanExecute(null));
            Assert.False(vm.BulkRemoveTagCommand.CanExecute(null));
        }

        [Fact]
        public async Task BulkAddTagCommand_CanExecute_False_WhenBulkTagInputEmpty()
        {
            File.WriteAllBytes(Path.Combine(_tempDir, "a.jpg"), new byte[] { 1 });
            var vm = new GalleryViewModel(CreateSetting()) { TargetDirectory = _tempDir };
            await vm.LoadCommand.ExecuteAsync(null);

            Assert.False(vm.BulkAddTagCommand.CanExecute(null));
            Assert.False(vm.BulkAddTagToStartCommand.CanExecute(null));
        }

        [Fact]
        public async Task BulkAddTagCommand_CanExecute_True_WhenImagesLoadedAndInputSet()
        {
            File.WriteAllBytes(Path.Combine(_tempDir, "a.jpg"), new byte[] { 1 });
            var vm = new GalleryViewModel(CreateSetting()) { TargetDirectory = _tempDir };
            await vm.LoadCommand.ExecuteAsync(null);
            vm.BulkTagInput = "new_tag";

            Assert.True(vm.BulkAddTagCommand.CanExecute(null));
            Assert.True(vm.BulkAddTagToStartCommand.CanExecute(null));
        }

        [Fact]
        public async Task BulkAddTagCommand_Execute_AddsTagToAllImages_AndClearsInput()
        {
            File.WriteAllBytes(Path.Combine(_tempDir, "a.jpg"), new byte[] { 1 });
            File.WriteAllBytes(Path.Combine(_tempDir, "b.jpg"), new byte[] { 1 });
            File.WriteAllText(Path.Combine(_tempDir, "b.txt"), "existing");
            var vm = new GalleryViewModel(CreateSetting()) { TargetDirectory = _tempDir };
            await vm.LoadCommand.ExecuteAsync(null);
            vm.BulkTagInput = "new_tag";

            vm.BulkAddTagCommand.Execute(null);

            Assert.All(vm.Images, entry => Assert.Contains("new_tag", entry.Tags));
            Assert.Equal("", vm.BulkTagInput);
        }

        [Fact]
        public async Task BulkAddTagToStartCommand_Execute_InsertsTagAtStartOfAllImages_AndClearsInput()
        {
            File.WriteAllBytes(Path.Combine(_tempDir, "a.jpg"), new byte[] { 1 });
            File.WriteAllText(Path.Combine(_tempDir, "a.txt"), "existing");
            var vm = new GalleryViewModel(CreateSetting()) { TargetDirectory = _tempDir };
            await vm.LoadCommand.ExecuteAsync(null);
            vm.BulkTagInput = "new_tag";

            vm.BulkAddTagToStartCommand.Execute(null);

            var entry = Assert.Single(vm.Images);
            Assert.Equal(new[] { "new_tag", "existing" }, entry.Tags);
            Assert.Equal("", vm.BulkTagInput);
        }

        [Fact]
        public async Task BulkRemoveTagCommand_Execute_RemovesTagFromAllImages_IgnoringCase()
        {
            File.WriteAllBytes(Path.Combine(_tempDir, "a.jpg"), new byte[] { 1 });
            File.WriteAllText(Path.Combine(_tempDir, "a.txt"), "Tag_A, tag_b");
            File.WriteAllBytes(Path.Combine(_tempDir, "b.jpg"), new byte[] { 1 });
            File.WriteAllText(Path.Combine(_tempDir, "b.txt"), "tag_b");
            var vm = new GalleryViewModel(CreateSetting()) { TargetDirectory = _tempDir };
            await vm.LoadCommand.ExecuteAsync(null);
            vm.BulkTagInput = "TAG_A";

            vm.BulkRemoveTagCommand.Execute(null);

            var entryA = vm.Images.Single(i => i.FileName == "a.jpg");
            var entryB = vm.Images.Single(i => i.FileName == "b.jpg");
            Assert.Equal(new[] { "tag_b" }, entryA.Tags);
            Assert.Equal(new[] { "tag_b" }, entryB.Tags);
            Assert.Equal("", vm.BulkTagInput);
        }

        // ── TagList（タグ候補一覧）の更新 ─────────────────────────────────────

        [Fact]
        public void Constructor_InitialState_TagListIsEmpty()
        {
            var vm = new GalleryViewModel(CreateSetting());

            Assert.Empty(vm.TagList);
        }

        [Fact]
        public async Task LoadCommand_Execute_ConfigPathNotSet_TagListRemainsEmpty()
        {
            File.WriteAllBytes(Path.Combine(_tempDir, "a.jpg"), new byte[] { 1 });
            var vm = new GalleryViewModel(CreateSetting()) { TargetDirectory = _tempDir };

            await vm.LoadCommand.ExecuteAsync(null);

            Assert.Empty(vm.TagList);
        }

        [Fact]
        public async Task LoadCommand_Execute_ValidConfig_PopulatesTagListFromReportFile()
        {
            File.WriteAllBytes(Path.Combine(_tempDir, "a.jpg"), new byte[] { 1 });
            File.WriteAllLines(
                Path.Combine(_tempDir, CaptioningService.ReportFileName),
                new[] { "1girl: 3", "solo: 2" });
            var vm = await CreateReadyVmAsync(new FakeCaptioningService());
            vm.TargetDirectory = _tempDir;

            await vm.LoadCommand.ExecuteAsync(null);

            Assert.Equal(new[] { "1girl", "solo" }, vm.TagList);
        }

        [Fact]
        public async Task LoadCommand_Execute_ReportGenerationThrows_TagListRemainsEmpty()
        {
            File.WriteAllBytes(Path.Combine(_tempDir, "a.jpg"), new byte[] { 1 });
            var fake = new FakeCaptioningService { ThrowOnGenerateReport = true };
            var vm = await CreateReadyVmAsync(fake);
            vm.TargetDirectory = _tempDir;

            await vm.LoadCommand.ExecuteAsync(null);

            Assert.Empty(vm.TagList);
        }

        [Fact]
        public async Task BulkAddTagCommand_Execute_RefreshesTagListFromReportFile()
        {
            File.WriteAllBytes(Path.Combine(_tempDir, "a.jpg"), new byte[] { 1 });
            var fake = new FakeCaptioningService();
            var vm = await CreateReadyVmAsync(fake);
            vm.TargetDirectory = _tempDir;
            await vm.LoadCommand.ExecuteAsync(null);
            vm.BulkTagInput = "new_tag";

            File.WriteAllLines(
                Path.Combine(_tempDir, CaptioningService.ReportFileName),
                new[] { "new_tag: 1" });

            await vm.BulkAddTagCommand.ExecuteAsync(null);

            Assert.Equal(new[] { "new_tag" }, vm.TagList);
        }

        [Fact]
        public async Task BulkAddTagToStartCommand_Execute_RefreshesTagListFromReportFile()
        {
            File.WriteAllBytes(Path.Combine(_tempDir, "a.jpg"), new byte[] { 1 });
            var fake = new FakeCaptioningService();
            var vm = await CreateReadyVmAsync(fake);
            vm.TargetDirectory = _tempDir;
            await vm.LoadCommand.ExecuteAsync(null);
            vm.BulkTagInput = "new_tag";

            File.WriteAllLines(
                Path.Combine(_tempDir, CaptioningService.ReportFileName),
                new[] { "new_tag: 1" });

            await vm.BulkAddTagToStartCommand.ExecuteAsync(null);

            Assert.Equal(new[] { "new_tag" }, vm.TagList);
        }

        [Fact]
        public async Task PerCardTagEdit_AfterLoad_RefreshesTagListFromReportFile()
        {
            File.WriteAllBytes(Path.Combine(_tempDir, "a.jpg"), new byte[] { 1 });
            var fake = new FakeCaptioningService();
            var vm = await CreateReadyVmAsync(fake);
            vm.TargetDirectory = _tempDir;
            await vm.LoadCommand.ExecuteAsync(null);
            var entry = Assert.Single(vm.Images);
            entry.NewTagInput = "card_tag";

            File.WriteAllLines(
                Path.Combine(_tempDir, CaptioningService.ReportFileName),
                new[] { "card_tag: 1" });

            await entry.AddNewTagCommand.ExecuteAsync(null);

            Assert.Equal(new[] { "card_tag" }, vm.TagList);
        }
    }
}
