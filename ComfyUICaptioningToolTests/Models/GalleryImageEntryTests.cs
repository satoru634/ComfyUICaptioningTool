using System.IO;
using System.Text.Json;
using System.Threading;
using ComfyUICaptioningTool.Models;
using ComfyUILibs.Models;

namespace ComfyUICaptioningToolTests.Models
{
    public class GalleryImageEntryTests : IDisposable
    {
        private readonly string _tempDir;

        public GalleryImageEntryTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose() => Directory.Delete(_tempDir, recursive: true);

        private string CreateImagePath(string fileName = "a.jpg")
        {
            var path = Path.Combine(_tempDir, fileName);
            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
            return path;
        }

        // ── コンストラクター ───────────────────────────────────────────────────

        [Fact]
        public void Constructor_SetsProperties()
        {
            var path = CreateImagePath();

            var entry = new GalleryImageEntry("a.jpg", path, new[] { "tag1" }, null);

            Assert.Equal("a.jpg", entry.FileName);
            Assert.Equal(path, entry.FullPath);
            Assert.Equal(new[] { "tag1" }, entry.Tags);
            Assert.Null(entry.Thumbnail);
            Assert.True(entry.HasTags);
        }

        [Fact]
        public void Constructor_NoTags_HasTagsIsFalse()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), Array.Empty<string>(), null);

            Assert.False(entry.HasTags);
        }

        // ── AddTag ────────────────────────────────────────────────────────────

        [Fact]
        public void AddTag_AddsTrimmedTag_AndWritesToTxt()
        {
            var path = CreateImagePath();
            var entry = new GalleryImageEntry("a.jpg", path, Array.Empty<string>(), null);

            entry.AddTag("  new_tag  ");

            Assert.Equal(new[] { "new_tag" }, entry.Tags);
            Assert.True(entry.HasTags);
            Assert.Equal("new_tag", File.ReadAllText(Path.ChangeExtension(path, ".txt")));
        }

        [Fact]
        public void AddTag_AppendsToExistingTags_WritesCommaJoined()
        {
            var path = CreateImagePath();
            var entry = new GalleryImageEntry("a.jpg", path, new[] { "existing" }, null);

            entry.AddTag("new_tag");

            Assert.Equal(new[] { "existing", "new_tag" }, entry.Tags);
            Assert.Equal("existing, new_tag", File.ReadAllText(Path.ChangeExtension(path, ".txt")));
        }

        [Fact]
        public void AddTag_EmptyOrWhitespace_DoesNotAdd()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), Array.Empty<string>(), null);

            entry.AddTag("   ");

            Assert.Empty(entry.Tags);
        }

        [Fact]
        public void AddTag_DuplicateIgnoringCase_DoesNotAdd()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "Tag_A" }, null);

            entry.AddTag("tag_a");

            Assert.Equal(new[] { "Tag_A" }, entry.Tags);
        }

        [Fact]
        public void AddTag_PrependTrue_InsertsAtStart_AndWritesCommaJoined()
        {
            var path = CreateImagePath();
            var entry = new GalleryImageEntry("a.jpg", path, new[] { "existing" }, null);

            entry.AddTag("new_tag", prepend: true);

            Assert.Equal(new[] { "new_tag", "existing" }, entry.Tags);
            Assert.Equal("new_tag, existing", File.ReadAllText(Path.ChangeExtension(path, ".txt")));
        }

        [Fact]
        public void AddTag_PrependTrue_DuplicateIgnoringCase_DoesNotAdd()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "Tag_A" }, null);

            entry.AddTag("tag_a", prepend: true);

            Assert.Equal(new[] { "Tag_A" }, entry.Tags);
        }

        // ── RemoveTag ─────────────────────────────────────────────────────────

        [Fact]
        public void RemoveTag_ExistingTag_RemovesAndUpdatesTxt()
        {
            var path = CreateImagePath();
            var entry = new GalleryImageEntry("a.jpg", path, new[] { "tag1", "tag2" }, null);

            entry.RemoveTag("tag1");

            Assert.Equal(new[] { "tag2" }, entry.Tags);
            Assert.Equal("tag2", File.ReadAllText(Path.ChangeExtension(path, ".txt")));
        }

        [Fact]
        public void RemoveTag_NotExisting_DoesNothing()
        {
            var path = CreateImagePath();
            var entry = new GalleryImageEntry("a.jpg", path, new[] { "tag1" }, null);

            entry.RemoveTag("nonexistent");

            Assert.Equal(new[] { "tag1" }, entry.Tags);
            Assert.False(File.Exists(Path.ChangeExtension(path, ".txt")));
        }

        [Fact]
        public void RemoveTag_LastTag_DeletesTxtFile()
        {
            var path = CreateImagePath();
            var txtPath = Path.ChangeExtension(path, ".txt");
            File.WriteAllText(txtPath, "tag1");
            var entry = new GalleryImageEntry("a.jpg", path, new[] { "tag1" }, null);

            entry.RemoveTag("tag1");

            Assert.False(entry.HasTags);
            Assert.False(File.Exists(txtPath));
        }

        // ── AddNewTagCommand ──────────────────────────────────────────────────

        [Fact]
        public void AddNewTagCommand_Execute_AddsInputAndClears()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), Array.Empty<string>(), null)
            {
                NewTagInput = "typed_tag",
            };

            entry.AddNewTagCommand.Execute(null);

            Assert.Equal(new[] { "typed_tag" }, entry.Tags);
            Assert.Equal("", entry.NewTagInput);
        }

        // ── AddNewTagToStartCommand ───────────────────────────────────────────

        [Fact]
        public void AddNewTagToStartCommand_Execute_InsertsInputAtStart_AndClears()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "existing" }, null)
            {
                NewTagInput = "typed_tag",
            };

            entry.AddNewTagToStartCommand.Execute(null);

            Assert.Equal(new[] { "typed_tag", "existing" }, entry.Tags);
            Assert.Equal("", entry.NewTagInput);
        }

        // ── TagList 更新コールバック（GalleryViewModel.RefreshTagListAsync 相当） ────

        [Fact]
        public async Task AddNewTagCommand_Execute_TagActuallyAdded_InvokesCallback()
        {
            var callbackCount = 0;
            var entry = new GalleryImageEntry(
                "a.jpg", CreateImagePath(), Array.Empty<string>(), null,
                onTagsChangedAsync: () => { callbackCount++; return Task.CompletedTask; })
            {
                NewTagInput = "typed_tag",
            };

            await entry.AddNewTagCommand.ExecuteAsync(null);

            Assert.Equal(1, callbackCount);
        }

        [Fact]
        public async Task AddNewTagCommand_Execute_EmptyInput_DoesNotInvokeCallback()
        {
            var callbackCount = 0;
            var entry = new GalleryImageEntry(
                "a.jpg", CreateImagePath(), Array.Empty<string>(), null,
                onTagsChangedAsync: () => { callbackCount++; return Task.CompletedTask; })
            {
                NewTagInput = "   ",
            };

            await entry.AddNewTagCommand.ExecuteAsync(null);

            Assert.Equal(0, callbackCount);
        }

        [Fact]
        public async Task AddNewTagToStartCommand_Execute_TagActuallyAdded_InvokesCallback()
        {
            var callbackCount = 0;
            var entry = new GalleryImageEntry(
                "a.jpg", CreateImagePath(), Array.Empty<string>(), null,
                onTagsChangedAsync: () => { callbackCount++; return Task.CompletedTask; })
            {
                NewTagInput = "typed_tag",
            };

            await entry.AddNewTagToStartCommand.ExecuteAsync(null);

            Assert.Equal(1, callbackCount);
        }

        [Fact]
        public async Task AddNewTagToStartCommand_Execute_EmptyInput_DoesNotInvokeCallback()
        {
            var callbackCount = 0;
            var entry = new GalleryImageEntry(
                "a.jpg", CreateImagePath(), Array.Empty<string>(), null,
                onTagsChangedAsync: () => { callbackCount++; return Task.CompletedTask; })
            {
                NewTagInput = "   ",
            };

            await entry.AddNewTagToStartCommand.ExecuteAsync(null);

            Assert.Equal(0, callbackCount);
        }

        [Fact]
        public async Task RemoveTagCommand_Execute_TagActuallyRemoved_InvokesCallback()
        {
            var callbackCount = 0;
            var entry = new GalleryImageEntry(
                "a.jpg", CreateImagePath(), new[] { "tag1" }, null,
                onTagsChangedAsync: () => { callbackCount++; return Task.CompletedTask; });

            await entry.RemoveTagCommand.ExecuteAsync("tag1");

            Assert.Equal(1, callbackCount);
        }

        [Fact]
        public async Task RemoveTagCommand_Execute_TagNotFound_DoesNotInvokeCallback()
        {
            var callbackCount = 0;
            var entry = new GalleryImageEntry(
                "a.jpg", CreateImagePath(), new[] { "tag1" }, null,
                onTagsChangedAsync: () => { callbackCount++; return Task.CompletedTask; });

            await entry.RemoveTagCommand.ExecuteAsync("nonexistent");

            Assert.Equal(0, callbackCount);
        }

        // ── captioning_config_result.json への反映 ───────────────────────────────

        private string ConfigResultPath => Path.Combine(_tempDir, "captioning_config_result.json");

        private WorkflowConfig ReadConfigResult()
            => JsonSerializer.Deserialize<WorkflowConfig>(
                File.ReadAllText(ConfigResultPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        [Fact]
        public void AddTag_ConfigResultNotExisting_CreatesFileWithPrependTags()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), Array.Empty<string>(), null);

            entry.AddTag("new_tag");

            var config = ReadConfigResult();
            Assert.Equal(new[] { "new_tag" }, config.PrependTags);
        }

        [Fact]
        public void AddTag_EmptyOrWhitespace_DoesNotCreateConfigResult()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), Array.Empty<string>(), null);

            entry.AddTag("   ");

            Assert.False(File.Exists(ConfigResultPath));
        }

        [Fact]
        public void AddTag_ExistingConfigResult_AppendsToPrependTags_KeepsOtherFields()
        {
            File.WriteAllText(ConfigResultPath, """
                {
                  "comfyui_url": "http://127.0.0.1:8188",
                  "prepend_tags": ["existing_tag"]
                }
                """);
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), Array.Empty<string>(), null);

            entry.AddTag("new_tag");

            var config = ReadConfigResult();
            Assert.Equal("http://127.0.0.1:8188", config.ComfyuiUrl);
            Assert.Equal(new[] { "existing_tag", "new_tag" }, config.PrependTags);
        }

        [Fact]
        public void AddTag_DuplicateInPrependTagsIgnoringCase_DoesNotAddAgain()
        {
            File.WriteAllText(ConfigResultPath, """{ "prepend_tags": ["Tag_A"] }""");
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), Array.Empty<string>(), null);

            entry.AddTag("tag_a");

            var config = ReadConfigResult();
            Assert.Equal(new[] { "Tag_A" }, config.PrependTags);
        }

        [Fact]
        public void AddTag_RemovesSameTagFromExcludeTags()
        {
            File.WriteAllText(ConfigResultPath, """{ "exclude_tags": ["new_tag", "other"] }""");
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), Array.Empty<string>(), null);

            entry.AddTag("new_tag");

            var config = ReadConfigResult();
            Assert.Equal(new[] { "other" }, config.ExcludeTags);
            Assert.Equal(new[] { "new_tag" }, config.PrependTags);
        }

        [Fact]
        public void RemoveTag_ConfigResultNotExisting_CreatesFileWithExcludeTags()
        {
            var path = CreateImagePath();
            var entry = new GalleryImageEntry("a.jpg", path, new[] { "tag1" }, null);

            entry.RemoveTag("tag1");

            var config = ReadConfigResult();
            Assert.Equal(new[] { "tag1" }, config.ExcludeTags);
        }

        [Fact]
        public void RemoveTag_NotExisting_DoesNotCreateConfigResult()
        {
            var path = CreateImagePath();
            var entry = new GalleryImageEntry("a.jpg", path, new[] { "tag1" }, null);

            entry.RemoveTag("nonexistent");

            Assert.False(File.Exists(ConfigResultPath));
        }

        [Fact]
        public void RemoveTag_RemovesSameTagFromPrependTags()
        {
            File.WriteAllText(ConfigResultPath, """{ "prepend_tags": ["tag1", "other"] }""");
            var path = CreateImagePath();
            var entry = new GalleryImageEntry("a.jpg", path, new[] { "tag1" }, null);

            entry.RemoveTag("tag1");

            var config = ReadConfigResult();
            Assert.Equal(new[] { "other" }, config.PrependTags);
            Assert.Equal(new[] { "tag1" }, config.ExcludeTags);
        }

        // ── ToggleTagSelectionCommand / SelectedTags ─────────────────────────────

        [Fact]
        public void ToggleTagSelectionCommand_Execute_NotSelected_SelectsTag()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1" }, null);

            entry.ToggleTagSelectionCommand.Execute("tag1");

            Assert.Equal(new[] { "tag1" }, entry.SelectedTags);
            Assert.True(entry.HasSelectedTags);
        }

        [Fact]
        public void ToggleTagSelectionCommand_Execute_AlreadySelected_DeselectsTag()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1" }, null);
            entry.ToggleTagSelectionCommand.Execute("tag1");

            entry.ToggleTagSelectionCommand.Execute("tag1");

            Assert.Empty(entry.SelectedTags);
            Assert.False(entry.HasSelectedTags);
        }

        [Fact]
        public void ToggleTagSelectionCommand_Execute_MultipleTags_AllRemainSelected()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1", "tag2" }, null);

            entry.ToggleTagSelectionCommand.Execute("tag1");
            entry.ToggleTagSelectionCommand.Execute("tag2");

            Assert.Equal(new[] { "tag1", "tag2" }, entry.SelectedTags);
        }

        // ── RemoveSelectedTagsCommand ─────────────────────────────────────────

        [Fact]
        public void RemoveSelectedTagsCommand_CanExecute_NoSelection_IsFalse()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1" }, null);

            Assert.False(entry.RemoveSelectedTagsCommand.CanExecute(null));
        }

        [Fact]
        public void RemoveSelectedTagsCommand_CanExecute_HasSelection_IsTrue()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1" }, null);
            entry.ToggleTagSelectionCommand.Execute("tag1");

            Assert.True(entry.RemoveSelectedTagsCommand.CanExecute(null));
        }

        [Fact]
        public async Task RemoveSelectedTagsCommand_Execute_RemovesAllSelectedTags_AndUpdatesTxt()
        {
            var path = CreateImagePath();
            var entry = new GalleryImageEntry("a.jpg", path, new[] { "tag1", "tag2", "tag3" }, null);
            entry.ToggleTagSelectionCommand.Execute("tag1");
            entry.ToggleTagSelectionCommand.Execute("tag3");

            await entry.RemoveSelectedTagsCommand.ExecuteAsync(null);

            Assert.Equal(new[] { "tag2" }, entry.Tags);
            Assert.Equal("tag2", File.ReadAllText(Path.ChangeExtension(path, ".txt")));
        }

        [Fact]
        public async Task RemoveSelectedTagsCommand_Execute_TagsRemoved_InvokesCallback()
        {
            var callbackCount = 0;
            var entry = new GalleryImageEntry(
                "a.jpg", CreateImagePath(), new[] { "tag1" }, null,
                onTagsChangedAsync: () => { callbackCount++; return Task.CompletedTask; });
            entry.ToggleTagSelectionCommand.Execute("tag1");

            await entry.RemoveSelectedTagsCommand.ExecuteAsync(null);

            Assert.Equal(1, callbackCount);
        }

        [Fact]
        public void RemoveSelectedTagsCommand_Execute_NoSelection_CannotExecute_TagsUnchanged()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1" }, null);

            Assert.False(entry.RemoveSelectedTagsCommand.CanExecute(null));
            Assert.Equal(new[] { "tag1" }, entry.Tags);
        }

        // ── CopyTagsToClipboardCommand ───────────────────────────────────────────

        /// <summary>
        /// Clipboard 操作は STA スレッドが必要なため、専用スレッドで実行する
        /// （MainWindowViewModelTests.RunOnSta と同じパターン）。
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
                    throw caught;
            }
        }

        [Fact]
        public void CopyTagsToClipboardCommand_Execute_CopiesCommaJoinedTags()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1", "tag2" }, null);

            string? clipboardText = null;
            RunOnSta(() =>
            {
                entry.CopyTagsToClipboardCommand.Execute(null);
                clipboardText = System.Windows.Clipboard.GetText();
            });

            Assert.Equal("tag1, tag2", clipboardText);
        }

        [Fact]
        public void CopyTagsToClipboardCommand_Execute_NoTags_DoesNotChangeClipboard()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), Array.Empty<string>(), null);

            string? clipboardText = null;
            RunOnSta(() =>
            {
                System.Windows.Clipboard.SetText("unchanged");
                entry.CopyTagsToClipboardCommand.Execute(null);
                clipboardText = System.Windows.Clipboard.GetText();
            });

            Assert.Equal("unchanged", clipboardText);
        }
    }
}
