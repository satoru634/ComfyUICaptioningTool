using System.IO;
using ComfyUICaptioningTool.Models;
using ComfyUICaptioningTool.Services;
using ComfyUILibs.Exceptions;
using ComfyUILibs.Services;

namespace ComfyUICaptioningToolTests.Services
{
    public class TaggerRunnerFactoryTests : IDisposable
    {
        private readonly string _tempDir;

        public TaggerRunnerFactoryTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
            EnsureTemplateFile();
        }

        public void Dispose() => Directory.Delete(_tempDir, recursive: true);

        /// <summary>
        /// Wd14TaggerRunner は AppDomain.CurrentDomain.BaseDirectory/templates を参照するため、
        /// テスト実行ディレクトリにテンプレートファイルを配置しておく（MainPageViewModelTests と同じ回避策）。
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

        private string WriteConfigFile(string json)
        {
            var path = Path.Combine(_tempDir, "captioning_config.json");
            File.WriteAllText(path, json);
            return path;
        }

        // ── ComfyUI バックエンド ──────────────────────────────────────────────

        [Fact]
        public void Create_ComfyUIBackend_ValidConfig_ReturnsWd14TaggerRunner()
        {
            var path = WriteConfigFile("""
                {
                  "comfyui_url": "http://127.0.0.1:8188",
                  "wd14_tagger": {
                    "model_name": "wd-eva02-large-tagger-v3",
                    "general_threshold": 0.35,
                    "character_threshold": 0.85
                  }
                }
                """);

            var runner = TaggerRunnerFactory.Create(path, TaggerBackend.ComfyUI);

            Assert.IsType<Wd14TaggerRunner>(runner);
        }

        [Fact]
        public void Create_ComfyUIBackend_MissingWd14TaggerSection_ThrowsComfyUIException()
        {
            var path = WriteConfigFile("""{ "comfyui_url": "http://127.0.0.1:8188" }""");

            Assert.Throws<ComfyUIException>(() => TaggerRunnerFactory.Create(path, TaggerBackend.ComfyUI));
        }

        // ── WdV3Timm バックエンド ─────────────────────────────────────────────

        [Fact]
        public void Create_WdV3TimmBackend_ValidConfig_ReturnsWdV3TimmTaggerRunner()
        {
            // wdv3-timm はモデル名・しきい値を wd14_tagger と共用し、実行ファイルは WdV3TimmPaths の
            // 固定パスを使う（captioning_config.json では扱わない）ため、wd14_tagger セクションのみで足りる
            var path = WriteConfigFile("""
                {
                  "wd14_tagger": {
                    "model_name": "wd-vit-tagger-v3",
                    "general_threshold": 0.35,
                    "character_threshold": 0.75
                  }
                }
                """);

            var runner = TaggerRunnerFactory.Create(path, TaggerBackend.WdV3Timm);

            Assert.IsType<WdV3TimmTaggerRunner>(runner);
        }

        [Fact]
        public void Create_WdV3TimmBackend_MissingWd14TaggerSection_ThrowsComfyUIException()
        {
            var path = WriteConfigFile("""{ "comfyui_url": "http://127.0.0.1:8188" }""");

            Assert.Throws<ComfyUIException>(() => TaggerRunnerFactory.Create(path, TaggerBackend.WdV3Timm));
        }
    }
}
