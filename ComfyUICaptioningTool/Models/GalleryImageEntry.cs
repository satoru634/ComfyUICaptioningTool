using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Windows;
using System.Windows.Media.Imaging;
using ComfyUILibs.Models;

namespace ComfyUICaptioningTool.Models
{
    /// <summary>
    /// GalleryPage の一覧表示用に、1 枚の画像とその同名 .txt から読み込んだタグ・サムネイルをまとめたもの。
    /// タグはカード上で追加・削除でき、変更のたびに同名 .txt へ即時保存する。
    /// 併せて、画像と同じディレクトリの captioning_config_result.json（次回タグ付け実行時に使用する
    /// 記録ファイル）へも、追加したタグは prepend_tags に・削除したタグは exclude_tags に反映する
    /// （MainPageViewModel が実行成功時に出力するのと同じファイル名・JSON フォーマット）。
    /// </summary>
    public partial class GalleryImageEntry : ObservableObject
    {
        /// <summary>captioning_config_result.json のファイル名（画像と同じディレクトリ直下）。</summary>
        private const string ConfigResultFileName = "captioning_config_result.json";

        /// <summary>captioning_config_result.json 読み込み時のオプション。プロパティ名の大文字/小文字を区別しない。</summary>
        private static readonly JsonSerializerOptions ConfigReadOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        /// <summary>
        /// captioning_config_result.json の書き込みオプション。null のプロパティを出力しないことで
        /// MainPageViewModel が出力するフォーマットと同じ見た目を維持する。
        /// </summary>
        private static readonly JsonSerializerOptions ConfigWriteOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        /// <summary>画像ファイル名（拡張子込み）。</summary>
        public string FileName { get; }

        /// <summary>画像ファイルのフルパス。</summary>
        public string FullPath { get; }

        /// <summary>同名 .txt から読み込んだタグ一覧（trim・空要素除去済み）。.txt が存在しない場合は空。</summary>
        public ObservableCollection<string> Tags { get; }

        /// <summary>縮小済みサムネイル。デコードに失敗した場合は null。</summary>
        public BitmapImage? Thumbnail { get; }

        /// <summary>タグが 1 つ以上存在するか。</summary>
        public bool HasTags => Tags.Count > 0;

        /// <summary>カード上の「タグを追加」用テキスト入力欄のバインド先。</summary>
        [ObservableProperty]
        private string _newTagInput = "";

        /// <summary>
        /// カード単位のタグ編集（<see cref="AddNewTagCommand"/>/<see cref="RemoveTagCommand"/>）が完了するたびに
        /// 呼び出されるコールバック（GalleryViewModel のタグ候補一覧 TagList の更新用）。null の場合は呼び出さない。
        /// 一括操作（GalleryViewModel.BulkAddTag/BulkRemoveTag）は <see cref="AddTag"/>/<see cref="RemoveTag"/> を
        /// 直接呼び出すためこのコールバックを経由しない（呼び出し元が一括操作完了後にまとめて更新する）。
        /// </summary>
        private readonly Func<Task>? _onTagsChangedAsync;

        public GalleryImageEntry(
            string fileName, string fullPath, IEnumerable<string> tags, BitmapImage? thumbnail,
            Func<Task>? onTagsChangedAsync = null)
        {
            FileName = fileName;
            FullPath = fullPath;
            Thumbnail = thumbnail;
            Tags = new ObservableCollection<string>(tags);
            Tags.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasTags));
            _onTagsChangedAsync = onTagsChangedAsync;
        }

        /// <summary>
        /// タグを追加する。前後の空白は trim し、大文字小文字を無視して既存タグと重複する場合・
        /// trim 後に空文字になる場合は追加しない。<paramref name="prepend"/> が true の場合は先頭に、
        /// それ以外は末尾に追加する。追加時は即座に同名 .txt へ保存し、
        /// captioning_config_result.json の prepend_tags へも反映する（exclude_tags 側に
        /// 同じタグがあれば矛盾しないよう取り除く）。実際に追加した場合は true を返す。
        /// </summary>
        public bool AddTag(string tag, bool prepend = false)
        {
            var trimmed = tag.Trim();
            if (trimmed.Length == 0)
                return false;
            if (Tags.Any(t => string.Equals(t, trimmed, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (prepend)
                Tags.Insert(0, trimmed);
            else
                Tags.Add(trimmed);
            SaveTags();

            UpdateConfigResult(config =>
            {
                config.PrependTags ??= new List<string>();
                if (!config.PrependTags.Any(t => string.Equals(t, trimmed, StringComparison.OrdinalIgnoreCase)))
                    config.PrependTags.Add(trimmed);

                config.ExcludeTags?.RemoveAll(t => string.Equals(t, trimmed, StringComparison.OrdinalIgnoreCase));
            });

            return true;
        }

        /// <summary>
        /// タグを削除する。削除できた場合は即座に同名 .txt へ保存し、captioning_config_result.json の
        /// exclude_tags へも反映する（prepend_tags 側に同じタグがあれば矛盾しないよう取り除く）。
        /// 実際に削除した場合は true を返す。
        /// </summary>
        public bool RemoveTag(string tag)
        {
            if (!Tags.Remove(tag))
                return false;

            SaveTags();

            UpdateConfigResult(config =>
            {
                config.ExcludeTags ??= new List<string>();
                if (!config.ExcludeTags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                    config.ExcludeTags.Add(tag);

                config.PrependTags?.RemoveAll(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
            });

            return true;
        }

        /// <summary>
        /// カードのタグチップ削除ボタン用コマンド。<see cref="RemoveTag"/> で実際に削除できた場合のみ、
        /// TagList 更新コールバックを呼び出す。
        /// </summary>
        [RelayCommand]
        private async Task RemoveTagAsync(string tag)
        {
            if (RemoveTag(tag) && _onTagsChangedAsync is not null)
                await _onTagsChangedAsync();
        }

        /// <summary>
        /// カード上の「タグを追加」入力欄（<see cref="NewTagInput"/>）の内容をタグとして末尾に追加し、
        /// 実際に追加できた場合のみ TagList 更新コールバックを呼び出す。
        /// </summary>
        [RelayCommand]
        private async Task AddNewTagAsync()
        {
            var added = AddTag(NewTagInput);
            NewTagInput = "";
            if (added && _onTagsChangedAsync is not null)
                await _onTagsChangedAsync();
        }

        /// <summary>
        /// カード上の「タグを追加」入力欄（<see cref="NewTagInput"/>）の内容をタグとして先頭に追加し、
        /// 実際に追加できた場合のみ TagList 更新コールバックを呼び出す。
        /// </summary>
        [RelayCommand]
        private async Task AddNewTagToStartAsync()
        {
            var added = AddTag(NewTagInput, prepend: true);
            NewTagInput = "";
            if (added && _onTagsChangedAsync is not null)
                await _onTagsChangedAsync();
        }

        /// <summary>
        /// 現在の <see cref="Tags"/> をカンマ区切りでクリップボードへコピーする。タグが 0 件の場合は何もしない。
        /// クリップボードアクセスに失敗した場合（他アプリによる一時的なロック等）は握りつぶす。
        /// </summary>
        [RelayCommand]
        private void CopyTagsToClipboard()
        {
            if (Tags.Count == 0)
                return;

            try
            {
                Clipboard.SetText(string.Join(", ", Tags));
            }
            catch
            {
                // クリップボードアクセス失敗時は静かに無視する
            }
        }

        /// <summary>
        /// 現在の <see cref="Tags"/> を同名 .txt へ書き込む。タグが 0 件になった場合は .txt 自体を削除する
        /// （空ファイルとして残さない）。
        /// </summary>
        private void SaveTags()
        {
            var txtPath = Path.ChangeExtension(FullPath, ".txt");
            if (Tags.Count == 0)
            {
                if (File.Exists(txtPath))
                    File.Delete(txtPath);
            }
            else
            {
                File.WriteAllText(txtPath, string.Join(", ", Tags), Encoding.UTF8);
            }
        }

        /// <summary>
        /// 画像と同じディレクトリの captioning_config_result.json を読み込み（存在しなければ新規）、
        /// <paramref name="update"/> で prepend_tags/exclude_tags を書き換えてから保存する。
        /// 読み込み・保存に失敗した場合は握りつぶす（タグ本体の .txt 保存はすでに完了しているため、
        /// この記録ファイルへの反映失敗でタグ編集自体を失敗扱いにはしない）。
        /// </summary>
        private void UpdateConfigResult(Action<WorkflowConfig> update)
        {
            try
            {
                var directory = Path.GetDirectoryName(FullPath);
                if (string.IsNullOrEmpty(directory))
                    return;

                var configPath = Path.Combine(directory, ConfigResultFileName);
                var config = File.Exists(configPath)
                    ? JsonSerializer.Deserialize<WorkflowConfig>(File.ReadAllText(configPath, Encoding.UTF8), ConfigReadOptions) ?? new WorkflowConfig()
                    : new WorkflowConfig();

                update(config);

                var json = JsonSerializer.Serialize(config, ConfigWriteOptions);
                File.WriteAllText(configPath, json, Encoding.UTF8);
            }
            catch
            {
                // captioning_config_result.json への反映失敗はタグ編集自体には影響させない
            }
        }
    }
}
