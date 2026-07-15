using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;
using ComfyUICaptioningTool.Helpers;
using ComfyUICaptioningTool.Models;
using ComfyUILibs.Common;

namespace ComfyUICaptioningTool.ViewModels.Pages
{
    /// <summary>
    /// GalleryPage の ViewModel。任意ディレクトリ内の画像と、同名 .txt から読み込んだタグを
    /// カード一覧として表示する。ComfyUI との通信は行わないため（ファイルシステムの走査のみ）、
    /// ConfigPage/ReportPage が使う ICaptioningService ファクトリー境界は不要。
    /// </summary>
    public partial class GalleryViewModel : ObservableObject
    {
        /// <summary>タグ付け対象とみなす画像ファイルの拡張子（大文字小文字は無視）。
        /// ComfyUILibs.Services.CaptioningService の同名一覧と揃えているが internal のため参照できず、
        /// GUI 側の表示専用ロジックとしてここに複製している。</summary>
        private static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        /// <summary>サムネイルのデコード幅（px）。メモリ使用量を抑えるため縮小デコードする。</summary>
        private const int ThumbnailDecodePixelWidth = 200;

        /// <summary>アプリケーション設定。</summary>
        public Setting<AppConfig> Config { get; }

        /// <summary>選択中の対象ディレクトリ。</summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
        private string? _targetDirectory;

        /// <summary>サブディレクトリも対象に含めるか。</summary>
        [ObservableProperty]
        private bool _recursive;

        /// <summary>読み込み中かどうか。</summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
        private bool _isLoading;

        /// <summary>状態メッセージ（ディレクトリ未存在・画像 0 件のいずれか。正常時は空文字）。</summary>
        [ObservableProperty]
        private string _statusMessage = "";

        /// <summary>読み込んだ画像とタグの一覧（ファイル名昇順）。</summary>
        [ObservableProperty]
        private ObservableCollection<GalleryImageEntry> _images = new();

        /// <summary>DI コンテナから設定を受け取って初期化する。</summary>
        public GalleryViewModel(Setting<AppConfig> config)
        {
            Config = config;
        }

        // ── ディレクトリ選択 ───────────────────────────────────────────────────

        /// <summary>フォルダー選択ダイアログを開いて対象ディレクトリを選択する。</summary>
        [RelayCommand]
        private void BrowseDirectory()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = LocalizationManager.Instance["Main_DirectoryDialogTitle"],
            };

            if (!string.IsNullOrWhiteSpace(TargetDirectory))
                dialog.InitialDirectory = TargetDirectory;

            if (dialog.ShowDialog() == true)
                TargetDirectory = dialog.FolderName;
        }

        // ── 画像・タグ読み込み ────────────────────────────────────────────────

        private bool CanLoad() => !IsLoading && !string.IsNullOrWhiteSpace(TargetDirectory);

        /// <summary>対象ディレクトリ内の画像とタグを読み込み、<see cref="Images"/> を更新する。</summary>
        [RelayCommand(CanExecute = nameof(CanLoad))]
        private async Task LoadAsync()
        {
            var directory = TargetDirectory!;

            Images = new ObservableCollection<GalleryImageEntry>();
            StatusMessage = "";

            if (!Directory.Exists(directory))
            {
                StatusMessage = string.Format(LocalizationManager.Instance["Gallery_FolderNotFound_Format"], directory);
                return;
            }

            IsLoading = true;

            try
            {
                var entries = await Task.Run(() => CollectEntries(directory, Recursive));
                Images = new ObservableCollection<GalleryImageEntry>(entries);

                if (entries.Count == 0)
                    StatusMessage = LocalizationManager.Instance["Gallery_NoImages"];
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>対象ディレクトリ内の画像ファイルを収集し、同名 .txt のタグとサムネイルを添えて返す（ファイル名昇順）。</summary>
        private static List<GalleryImageEntry> CollectEntries(string directory, bool recursive)
        {
            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var imagePaths = Directory.EnumerateFiles(directory, "*", option)
                .Where(f => SupportedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

            var entries = new List<GalleryImageEntry>();
            foreach (var imagePath in imagePaths)
            {
                var txtPath = Path.ChangeExtension(imagePath, ".txt");
                var tags = File.Exists(txtPath)
                    ? SplitTags(File.ReadAllText(txtPath, Encoding.UTF8))
                    : new List<string>();

                entries.Add(new GalleryImageEntry(
                    Path.GetFileName(imagePath), imagePath, tags, TryCreateThumbnail(imagePath)));
            }

            return entries;
        }

        /// <summary>カンマ区切りタグ文字列を trim・空要素除去したリストに分割する。</summary>
        private static List<string> SplitTags(string text)
            => text.Split(',')
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .ToList();

        /// <summary>
        /// 画像ファイルから縮小済みサムネイルを生成する。デコードに失敗した場合（画像として不正なファイル等）は
        /// null を返し、呼び出し元では一覧表示自体は継続する。
        /// </summary>
        private static BitmapImage? TryCreateThumbnail(string imagePath)
        {
            try
            {
                var bytes = File.ReadAllBytes(imagePath);
                using var stream = new MemoryStream(bytes);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = ThumbnailDecodePixelWidth;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}
