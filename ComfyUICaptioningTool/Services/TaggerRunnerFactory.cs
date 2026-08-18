using ComfyUICaptioningTool.Models;
using ComfyUILibs.Services;

namespace ComfyUICaptioningTool.Services
{
    /// <summary>
    /// captioning_config.json のパスと選択中の <see cref="TaggerBackend"/> から
    /// <see cref="ITaggerRunner"/> の実装を構築する静的ファクトリー。
    /// </summary>
    public static class TaggerRunnerFactory
    {
        /// <summary>
        /// <paramref name="backend"/> が <see cref="TaggerBackend.WdV3Timm"/> の場合は
        /// <see cref="WdV3TimmTaggerRunner"/>（ローカル wdv3-timm 常駐プロセス経由、ComfyUI 不要）、
        /// それ以外（既定 <see cref="TaggerBackend.ComfyUI"/>）は <see cref="Wd14TaggerRunner"/>
        /// （ComfyUI 経由）を構築する。
        /// </summary>
        /// <param name="configPath">captioning_config.json のパス。</param>
        /// <param name="backend">使用するタグ付けバックエンド。</param>
        /// <exception cref="ComfyUILibs.Exceptions.ComfyUIException">
        /// captioning_config.json に該当バックエンドのセクションが無い、または値が不正な場合。
        /// </exception>
        public static ITaggerRunner Create(string configPath, TaggerBackend backend)
            => backend switch
            {
                TaggerBackend.WdV3Timm => new WdV3TimmTaggerRunner(configPath),
                _ => new Wd14TaggerRunner(configPath),
            };
    }
}
