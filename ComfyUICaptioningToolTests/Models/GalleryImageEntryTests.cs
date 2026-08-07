using System.IO;
using System.Linq;
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

        // ── 既存タグ入力時の選択への切替（SelectExistingTag） ─────────────────

        [Fact]
        public void AddNewTagCommand_Execute_ExistingTagIgnoringCase_SelectsInsteadOfAdding()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "Tag_A" }, null)
            {
                NewTagInput = "  tag_a  ",
            };

            entry.AddNewTagCommand.Execute(null);

            Assert.Equal(new[] { "Tag_A" }, entry.Tags);
            Assert.Equal(new[] { "Tag_A" }, entry.SelectedTags);
            Assert.Equal("", entry.NewTagInput);
        }

        [Fact]
        public void AddNewTagCommand_Execute_ExistingTag_DoesNotRewriteTxt()
        {
            var path = CreateImagePath();
            var txtPath = Path.ChangeExtension(path, ".txt");
            var entry = new GalleryImageEntry("a.jpg", path, new[] { "tag1" }, null)
            {
                NewTagInput = "tag1",
            };

            entry.AddNewTagCommand.Execute(null);

            Assert.False(File.Exists(txtPath));
        }

        [Fact]
        public void AddNewTagToStartCommand_Execute_ExistingTagIgnoringCase_SelectsInsteadOfAdding()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "existing", "Tag_A" }, null)
            {
                NewTagInput = "tag_a",
            };

            entry.AddNewTagToStartCommand.Execute(null);

            Assert.Equal(new[] { "existing", "Tag_A" }, entry.Tags);
            Assert.Equal(new[] { "Tag_A" }, entry.SelectedTags);
            Assert.Equal("", entry.NewTagInput);
        }

        [Fact]
        public void AddNewTagCommand_Execute_ExistingTagAlreadySelected_StaysSelected()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1" }, null);
            entry.ToggleTagSelectionCommand.Execute("tag1");

            entry.NewTagInput = "tag1";
            entry.AddNewTagCommand.Execute(null);

            Assert.Equal(new[] { "tag1" }, entry.SelectedTags);
        }

        [Fact]
        public async Task AddNewTagCommand_Execute_ExistingTag_DoesNotInvokeCallback()
        {
            var callbackCount = 0;
            var entry = new GalleryImageEntry(
                "a.jpg", CreateImagePath(), new[] { "tag1" }, null,
                onTagsChangedAsync: () => { callbackCount++; return Task.CompletedTask; })
            {
                NewTagInput = "tag1",
            };

            await entry.AddNewTagCommand.ExecuteAsync(null);

            Assert.Equal(0, callbackCount);
        }

        [Fact]
        public void AddNewTagCommand_Execute_ExistingTag_EnablesRemoveSelectedTagsCommand()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1" }, null)
            {
                NewTagInput = "tag1",
            };
            Assert.False(entry.RemoveSelectedTagsCommand.CanExecute(null));

            entry.AddNewTagCommand.Execute(null);

            Assert.True(entry.RemoveSelectedTagsCommand.CanExecute(null));
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

        // ── SelectedTags 変更時の CanExecuteChanged 通知 ─────────────────────────
        // WPF のボタンは CanExecuteChanged イベントが発火して初めて IsEnabled を再評価するため、
        // CanExecute(null) を直接呼ぶだけのテストでは「選択してもボタンが有効化されない」不具合を
        // 検出できない（ToggleTagSelectionCommand の SelectedTags.CollectionChanged ハンドラー内で
        // 各コマンドの NotifyCanExecuteChanged() 呼び出しが漏れていた実際の不具合を踏まえ追加）。

        [Theory]
        [InlineData(nameof(GalleryImageEntry.RemoveSelectedTagsCommand))]
        [InlineData(nameof(GalleryImageEntry.MoveSelectedTagsToStartCommand))]
        [InlineData(nameof(GalleryImageEntry.MoveSelectedTagsToEndCommand))]
        [InlineData(nameof(GalleryImageEntry.MoveSelectedTagsUpCommand))]
        [InlineData(nameof(GalleryImageEntry.MoveSelectedTagsDownCommand))]
        public void SelectionCommands_ToggleTagSelection_RaisesCanExecuteChanged(string commandPropertyName)
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1" }, null);
            var command = (System.Windows.Input.ICommand)typeof(GalleryImageEntry)
                .GetProperty(commandPropertyName)!.GetValue(entry)!;

            var raised = false;
            command.CanExecuteChanged += (_, _) => raised = true;

            entry.ToggleTagSelectionCommand.Execute("tag1");

            Assert.True(raised);
        }

        // ── MoveSelectedTagsToStartCommand / MoveSelectedTagsToEndCommand ────────

        [Fact]
        public void MoveSelectedTagsToStartCommand_CanExecute_NoSelection_IsFalse()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1" }, null);

            Assert.False(entry.MoveSelectedTagsToStartCommand.CanExecute(null));
        }

        [Fact]
        public void MoveSelectedTagsToStartCommand_Execute_MovesSelectedTagsToStart_PreservingRelativeOrder_AndUpdatesTxt()
        {
            var path = CreateImagePath();
            var entry = new GalleryImageEntry("a.jpg", path, new[] { "tag1", "tag2", "tag3", "tag4" }, null);
            entry.ToggleTagSelectionCommand.Execute("tag2");
            entry.ToggleTagSelectionCommand.Execute("tag4");

            entry.MoveSelectedTagsToStartCommand.Execute(null);

            Assert.Equal(new[] { "tag2", "tag4", "tag1", "tag3" }, entry.Tags);
            Assert.Equal("tag2, tag4, tag1, tag3", File.ReadAllText(Path.ChangeExtension(path, ".txt")));
        }

        [Fact]
        public void MoveSelectedTagsToEndCommand_Execute_MovesSelectedTagsToEnd_PreservingRelativeOrder_AndUpdatesTxt()
        {
            var path = CreateImagePath();
            var entry = new GalleryImageEntry("a.jpg", path, new[] { "tag1", "tag2", "tag3", "tag4" }, null);
            entry.ToggleTagSelectionCommand.Execute("tag1");
            entry.ToggleTagSelectionCommand.Execute("tag3");

            entry.MoveSelectedTagsToEndCommand.Execute(null);

            Assert.Equal(new[] { "tag2", "tag4", "tag1", "tag3" }, entry.Tags);
            Assert.Equal("tag2, tag4, tag1, tag3", File.ReadAllText(Path.ChangeExtension(path, ".txt")));
        }

        [Fact]
        public void MoveSelectedTagsToStartCommand_Execute_NoSelection_TagsUnchanged()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1", "tag2" }, null);

            Assert.False(entry.MoveSelectedTagsToStartCommand.CanExecute(null));
            Assert.Equal(new[] { "tag1", "tag2" }, entry.Tags);
        }

        // ── MoveSelectedTagsUpCommand / MoveSelectedTagsDownCommand ──────────────

        [Fact]
        public void MoveSelectedTagsUpCommand_CanExecute_NoSelection_IsFalse()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1" }, null);

            Assert.False(entry.MoveSelectedTagsUpCommand.CanExecute(null));
        }

        [Fact]
        public void MoveSelectedTagsUpCommand_Execute_SingleSelectedTag_SwapsWithPrevious_AndUpdatesTxt()
        {
            var path = CreateImagePath();
            var entry = new GalleryImageEntry("a.jpg", path, new[] { "tag1", "tag2", "tag3" }, null);
            entry.ToggleTagSelectionCommand.Execute("tag2");

            entry.MoveSelectedTagsUpCommand.Execute(null);

            Assert.Equal(new[] { "tag2", "tag1", "tag3" }, entry.Tags);
            Assert.Equal("tag2, tag1, tag3", File.ReadAllText(Path.ChangeExtension(path, ".txt")));
        }

        [Fact]
        public void MoveSelectedTagsUpCommand_Execute_TagAlreadyAtStart_NoChange()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1", "tag2" }, null);
            entry.ToggleTagSelectionCommand.Execute("tag1");

            entry.MoveSelectedTagsUpCommand.Execute(null);

            Assert.Equal(new[] { "tag1", "tag2" }, entry.Tags);
        }

        [Fact]
        public void MoveSelectedTagsUpCommand_Execute_ContiguousBlock_MovesTogetherAsBlock()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1", "tag2", "tag3", "tag4" }, null);
            entry.ToggleTagSelectionCommand.Execute("tag2");
            entry.ToggleTagSelectionCommand.Execute("tag3");

            entry.MoveSelectedTagsUpCommand.Execute(null);

            Assert.Equal(new[] { "tag2", "tag3", "tag1", "tag4" }, entry.Tags);
        }

        [Fact]
        public void MoveSelectedTagsDownCommand_Execute_SingleSelectedTag_SwapsWithNext_AndUpdatesTxt()
        {
            var path = CreateImagePath();
            var entry = new GalleryImageEntry("a.jpg", path, new[] { "tag1", "tag2", "tag3" }, null);
            entry.ToggleTagSelectionCommand.Execute("tag2");

            entry.MoveSelectedTagsDownCommand.Execute(null);

            Assert.Equal(new[] { "tag1", "tag3", "tag2" }, entry.Tags);
            Assert.Equal("tag1, tag3, tag2", File.ReadAllText(Path.ChangeExtension(path, ".txt")));
        }

        [Fact]
        public void MoveSelectedTagsDownCommand_Execute_TagAlreadyAtEnd_NoChange()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1", "tag2" }, null);
            entry.ToggleTagSelectionCommand.Execute("tag2");

            entry.MoveSelectedTagsDownCommand.Execute(null);

            Assert.Equal(new[] { "tag1", "tag2" }, entry.Tags);
        }

        [Fact]
        public void MoveSelectedTagsDownCommand_Execute_ContiguousBlock_MovesTogetherAsBlock()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1", "tag2", "tag3", "tag4" }, null);
            entry.ToggleTagSelectionCommand.Execute("tag2");
            entry.ToggleTagSelectionCommand.Execute("tag3");

            entry.MoveSelectedTagsDownCommand.Execute(null);

            Assert.Equal(new[] { "tag1", "tag4", "tag2", "tag3" }, entry.Tags);
        }

        // ── gallery_edit_log.jsonl への作業ログ記録 ──────────────────────────────

        private string EditLogPath => Path.Combine(_tempDir, "gallery_edit_log.jsonl");

        private List<GalleryEditLogEntry> ReadEditLog()
            => File.Exists(EditLogPath)
                ? File.ReadAllLines(EditLogPath)
                    .Where(line => line.Length > 0)
                    .Select(line => JsonSerializer.Deserialize<GalleryEditLogEntry>(line)!)
                    .ToList()
                : new List<GalleryEditLogEntry>();

        [Fact]
        public void AddTag_End_AppendsEditLogEntry()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), Array.Empty<string>(), null);

            entry.AddTag("new_tag");

            var log = ReadEditLog();
            var logEntry = Assert.Single(log);
            Assert.Equal("a.jpg", logEntry.FileName);
            Assert.Equal("add_end", logEntry.Operation);
            Assert.Equal(new[] { "new_tag" }, logEntry.Tags);
        }

        [Fact]
        public void AddTag_Prepend_AppendsEditLogEntry_WithAddStartOperation()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), Array.Empty<string>(), null);

            entry.AddTag("new_tag", prepend: true);

            var logEntry = Assert.Single(ReadEditLog());
            Assert.Equal("add_start", logEntry.Operation);
            Assert.Equal(new[] { "new_tag" }, logEntry.Tags);
        }

        [Fact]
        public void AddTag_EmptyOrWhitespace_DoesNotAppendEditLog()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), Array.Empty<string>(), null);

            entry.AddTag("   ");

            Assert.False(File.Exists(EditLogPath));
        }

        [Fact]
        public void AddTag_DuplicateIgnoringCase_DoesNotAppendEditLog()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "Tag_A" }, null);

            entry.AddTag("tag_a");

            Assert.False(File.Exists(EditLogPath));
        }

        [Fact]
        public void RemoveTag_ExistingTag_AppendsEditLogEntry()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1" }, null);

            entry.RemoveTag("tag1");

            var logEntry = Assert.Single(ReadEditLog());
            Assert.Equal("a.jpg", logEntry.FileName);
            Assert.Equal("remove", logEntry.Operation);
            Assert.Equal(new[] { "tag1" }, logEntry.Tags);
        }

        [Fact]
        public void RemoveTag_NotExisting_DoesNotAppendEditLog()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1" }, null);

            entry.RemoveTag("nonexistent");

            Assert.False(File.Exists(EditLogPath));
        }

        [Fact]
        public void MultipleOperations_AppendEditLogEntries_InOrder()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), Array.Empty<string>(), null);

            entry.AddTag("tag1");
            entry.AddTag("tag2", prepend: true);
            entry.RemoveTag("tag1");

            var log = ReadEditLog();
            Assert.Equal(3, log.Count);
            Assert.Equal("add_end", log[0].Operation);
            Assert.Equal("add_start", log[1].Operation);
            Assert.Equal("remove", log[2].Operation);
        }

        [Fact]
        public void MoveSelectedTagsToStartCommand_Execute_AppendsReorderToStartEntry()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1", "tag2", "tag3" }, null);
            entry.ToggleTagSelectionCommand.Execute("tag3");

            entry.MoveSelectedTagsToStartCommand.Execute(null);

            var logEntry = Assert.Single(ReadEditLog());
            Assert.Equal("reorder_to_start", logEntry.Operation);
            Assert.Equal(new[] { "tag3" }, logEntry.Tags);
        }

        [Fact]
        public void MoveSelectedTagsToStartCommand_Execute_AlreadyAtStart_DoesNotAppendEditLog()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1", "tag2" }, null);
            entry.ToggleTagSelectionCommand.Execute("tag1");

            entry.MoveSelectedTagsToStartCommand.Execute(null);

            Assert.False(File.Exists(EditLogPath));
        }

        [Fact]
        public void MoveSelectedTagsToEndCommand_Execute_AppendsReorderToEndEntry()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1", "tag2", "tag3" }, null);
            entry.ToggleTagSelectionCommand.Execute("tag1");

            entry.MoveSelectedTagsToEndCommand.Execute(null);

            var logEntry = Assert.Single(ReadEditLog());
            Assert.Equal("reorder_to_end", logEntry.Operation);
            Assert.Equal(new[] { "tag1" }, logEntry.Tags);
        }

        [Fact]
        public void MoveSelectedTagsToEndCommand_Execute_AlreadyAtEnd_DoesNotAppendEditLog()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1", "tag2" }, null);
            entry.ToggleTagSelectionCommand.Execute("tag2");

            entry.MoveSelectedTagsToEndCommand.Execute(null);

            Assert.False(File.Exists(EditLogPath));
        }

        [Fact]
        public void MoveSelectedTagsUpCommand_Execute_AppendsReorderUpEntry()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1", "tag2" }, null);
            entry.ToggleTagSelectionCommand.Execute("tag2");

            entry.MoveSelectedTagsUpCommand.Execute(null);

            var logEntry = Assert.Single(ReadEditLog());
            Assert.Equal("reorder_up", logEntry.Operation);
            Assert.Equal(new[] { "tag2" }, logEntry.Tags);
        }

        [Fact]
        public void MoveSelectedTagsUpCommand_Execute_TagAlreadyAtStart_DoesNotAppendEditLog()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1", "tag2" }, null);
            entry.ToggleTagSelectionCommand.Execute("tag1");

            entry.MoveSelectedTagsUpCommand.Execute(null);

            Assert.False(File.Exists(EditLogPath));
        }

        [Fact]
        public void MoveSelectedTagsDownCommand_Execute_AppendsReorderDownEntry()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1", "tag2" }, null);
            entry.ToggleTagSelectionCommand.Execute("tag1");

            entry.MoveSelectedTagsDownCommand.Execute(null);

            var logEntry = Assert.Single(ReadEditLog());
            Assert.Equal("reorder_down", logEntry.Operation);
            Assert.Equal(new[] { "tag1" }, logEntry.Tags);
        }

        [Fact]
        public void MoveSelectedTagsDownCommand_Execute_TagAlreadyAtEnd_DoesNotAppendEditLog()
        {
            var entry = new GalleryImageEntry("a.jpg", CreateImagePath(), new[] { "tag1", "tag2" }, null);
            entry.ToggleTagSelectionCommand.Execute("tag2");

            entry.MoveSelectedTagsDownCommand.Execute(null);

            Assert.False(File.Exists(EditLogPath));
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
