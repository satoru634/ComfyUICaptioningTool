using ComfyUILibs.Models;
using ComfyUILibs.Services;

namespace ComfyUICaptioningTool.Services
{
    /// <summary>
    /// <see cref="ComfyUILibs.Services.CaptioningService"/> を <see cref="ICaptioningService"/> として公開するアダプター。
    /// </summary>
    public class CaptioningServiceAdapter : ICaptioningService
    {
        private readonly CaptioningService _inner;

        /// <summary>実体となる <see cref="Wd14TaggerRunner"/> と prepend/exclude タグを受け取って初期化する。</summary>
        public CaptioningServiceAdapter(
            Wd14TaggerRunner taggerRunner,
            IReadOnlyList<string>? prependTags = null,
            IReadOnlyList<string>? excludeTags = null)
        {
            _inner = new CaptioningService(taggerRunner, prependTags, excludeTags);
        }

        /// <inheritdoc/>
        public Task<(int Processed, int Skipped, int Errors)> ProcessDirectoryAsync(
            string directory,
            bool recursive,
            bool overwrite,
            IProgress<CaptioningProgress>? progress = null)
            => _inner.ProcessDirectoryAsync(directory, recursive, overwrite, progress);

        /// <inheritdoc/>
        public Task GenerateReportAsync(string directory, bool recursive)
            => _inner.GenerateReportAsync(directory, recursive);
    }
}
