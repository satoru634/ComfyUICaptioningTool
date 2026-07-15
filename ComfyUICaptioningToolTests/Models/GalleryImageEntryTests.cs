using System.IO;
using ComfyUICaptioningTool.Models;

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
    }
}
