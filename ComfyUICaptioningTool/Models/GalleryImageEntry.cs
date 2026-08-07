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
    /// さらに、追加・削除・並び替え（実際に変更が生じた場合のみ）のたびに、画像と同じディレクトリの
    /// gallery_edit_log.jsonl へ作業ログを 1 行追記する。
    /// </summary>
    public partial class GalleryImageEntry : ObservableObject
    {
        /// <summary>captioning_config_result.json のファイル名（画像と同じディレクトリ直下）。</summary>
        private const string ConfigResultFileName = "captioning_config_result.json";

        /// <summary>タグ操作の作業ログファイル名（画像と同じディレクトリ直下、JSON Lines 形式）。</summary>
        private const string EditLogFileName = "gallery_edit_log.jsonl";

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

        /// <summary>gallery_edit_log.jsonl の書き込みオプション。1 行 1 エントリのため改行を含めない。</summary>
        private static readonly JsonSerializerOptions EditLogWriteOptions = new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
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

        /// <summary>タグ名ボタン（トグルボタン）で選択中のタグ一覧（複数選択可）。</summary>
        public ObservableCollection<string> SelectedTags { get; } = new();

        /// <summary>選択中のタグが 1 つ以上あるか（削除ボタンの活性制御用）。</summary>
        public bool HasSelectedTags => SelectedTags.Count > 0;

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
            SelectedTags.CollectionChanged += (_, _) =>
            {
                // SelectedTags 自体は get-only の固定インスタンスのため、内容変更時も
                // OnPropertyChanged(nameof(SelectedTags)) を明示的に発火しないと、XAML 側の
                // MultiBinding（各タグボタンの IsChecked、SelectedTags を参照）が再評価されない
                OnPropertyChanged(nameof(SelectedTags));
                OnPropertyChanged(nameof(HasSelectedTags));
                RemoveSelectedTagsCommand.NotifyCanExecuteChanged();
                MoveSelectedTagsToStartCommand.NotifyCanExecuteChanged();
                MoveSelectedTagsToEndCommand.NotifyCanExecuteChanged();
                MoveSelectedTagsUpCommand.NotifyCanExecuteChanged();
                MoveSelectedTagsDownCommand.NotifyCanExecuteChanged();
            };
            _onTagsChangedAsync = onTagsChangedAsync;
        }

        /// <summary>
        /// タグ名ボタン（トグルボタン）のクリックで選択状態をトグルする。選択中なら解除し、
        /// 未選択なら選択に加える（複数選択可）。
        /// </summary>
        [RelayCommand]
        private void ToggleTagSelection(string tag)
        {
            if (!SelectedTags.Remove(tag))
                SelectedTags.Add(tag);
        }

        /// <summary>
        /// 選択中の全タグ（<see cref="SelectedTags"/>）を削除する。1 件以上実際に削除できた場合のみ
        /// TagList 更新コールバックを呼び出す。削除後の選択状態は特に整理しない（削除されたタグは
        /// 一覧から消えるため、トグルボタン自体が表示されなくなる）。
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelectedTags))]
        private async Task RemoveSelectedTagsAsync()
        {
            var anyRemoved = false;
            foreach (var tag in SelectedTags.ToList())
            {
                if (RemoveTag(tag))
                    anyRemoved = true;
            }

            if (anyRemoved && _onTagsChangedAsync is not null)
                await _onTagsChangedAsync();
        }

        /// <summary>
        /// 選択中の全タグ（<see cref="SelectedTags"/>）を、相対順序を保ったまま先頭へまとめて移動する。
        /// 同名 .txt へ即座に保存する。
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelectedTags))]
        private void MoveSelectedTagsToStart()
        {
            var selected = Tags.Where(t => SelectedTags.Contains(t)).ToList();
            if (selected.Count == 0)
                return;

            var before = Tags.ToList();
            foreach (var tag in selected)
                Tags.Remove(tag);
            for (var i = 0; i < selected.Count; i++)
                Tags.Insert(i, selected[i]);

            SaveTags();
            if (!Tags.SequenceEqual(before))
                LogEdit("reorder_to_start", selected);
        }

        /// <summary>
        /// 選択中の全タグ（<see cref="SelectedTags"/>）を、相対順序を保ったまま末尾へまとめて移動する。
        /// 同名 .txt へ即座に保存する。
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelectedTags))]
        private void MoveSelectedTagsToEnd()
        {
            var selected = Tags.Where(t => SelectedTags.Contains(t)).ToList();
            if (selected.Count == 0)
                return;

            var before = Tags.ToList();
            foreach (var tag in selected)
                Tags.Remove(tag);
            foreach (var tag in selected)
                Tags.Add(tag);

            SaveTags();
            if (!Tags.SequenceEqual(before))
                LogEdit("reorder_to_end", selected);
        }

        /// <summary>
        /// 選択中の各タグを、直前の要素が非選択タグの場合に限り 1 つ前へ移動する（先頭から順に処理することで、
        /// 連続して選択されたタグのまとまりはブロックとして一緒に 1 つ前へ移動する）。同名 .txt へ即座に保存する。
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelectedTags))]
        private void MoveSelectedTagsUp()
        {
            var before = Tags.ToList();
            for (var i = 1; i < Tags.Count; i++)
            {
                if (SelectedTags.Contains(Tags[i]) && !SelectedTags.Contains(Tags[i - 1]))
                    Tags.Move(i, i - 1);
            }

            SaveTags();
            if (!Tags.SequenceEqual(before))
                LogEdit("reorder_up", Tags.Where(t => SelectedTags.Contains(t)).ToList());
        }

        /// <summary>
        /// 選択中の各タグを、直後の要素が非選択タグの場合に限り 1 つ後ろへ移動する（末尾から順に処理することで、
        /// 連続して選択されたタグのまとまりはブロックとして一緒に 1 つ後ろへ移動する）。同名 .txt へ即座に保存する。
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelectedTags))]
        private void MoveSelectedTagsDown()
        {
            var before = Tags.ToList();
            for (var i = Tags.Count - 2; i >= 0; i--)
            {
                if (SelectedTags.Contains(Tags[i]) && !SelectedTags.Contains(Tags[i + 1]))
                    Tags.Move(i, i + 1);
            }

            SaveTags();
            if (!Tags.SequenceEqual(before))
                LogEdit("reorder_down", Tags.Where(t => SelectedTags.Contains(t)).ToList());
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

            LogEdit(prepend ? "add_start" : "add_end", new[] { trimmed });

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
            SelectedTags.Remove(tag);

            UpdateConfigResult(config =>
            {
                config.ExcludeTags ??= new List<string>();
                if (!config.ExcludeTags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                    config.ExcludeTags.Add(tag);

                config.PrependTags?.RemoveAll(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
            });

            LogEdit("remove", new[] { tag });

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
        /// 実際に追加できた場合のみ TagList 更新コールバックを呼び出す。入力内容が既存タグと
        /// 大文字小文字無視で一致する場合は追加を行わず、代わりに <see cref="SelectExistingTag"/> で
        /// そのタグを選択状態にする（トグルボタンを有効にする）。
        /// </summary>
        [RelayCommand]
        private async Task AddNewTagAsync()
        {
            var input = NewTagInput;
            NewTagInput = "";

            if (SelectExistingTag(input))
                return;

            var added = AddTag(input);
            if (added && _onTagsChangedAsync is not null)
                await _onTagsChangedAsync();
        }

        /// <summary>
        /// カード上の「タグを追加」入力欄（<see cref="NewTagInput"/>）の内容をタグとして先頭に追加し、
        /// 実際に追加できた場合のみ TagList 更新コールバックを呼び出す。入力内容が既存タグと
        /// 大文字小文字無視で一致する場合は追加を行わず、代わりに <see cref="SelectExistingTag"/> で
        /// そのタグを選択状態にする（トグルボタンを有効にする）。
        /// </summary>
        [RelayCommand]
        private async Task AddNewTagToStartAsync()
        {
            var input = NewTagInput;
            NewTagInput = "";

            if (SelectExistingTag(input))
                return;

            var added = AddTag(input, prepend: true);
            if (added && _onTagsChangedAsync is not null)
                await _onTagsChangedAsync();
        }

        /// <summary>
        /// 入力内容（trim 済み）が既存の <see cref="Tags"/> と大文字小文字無視で一致する場合、そのタグを
        /// <see cref="SelectedTags"/> へ選択状態として加える（トグルボタンを有効にする）。既に選択済みの
        /// 場合は何もしない。選択状態が変わると <see cref="RemoveSelectedTagsCommand"/>/並び替え系コマンドの
        /// CanExecute も自動的に再評価される（SelectedTags.CollectionChanged 経由）。一致した場合は true を返す。
        /// </summary>
        private bool SelectExistingTag(string input)
        {
            var trimmed = input.Trim();
            if (trimmed.Length == 0)
                return false;

            var existing = Tags.FirstOrDefault(t => string.Equals(t, trimmed, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
                return false;

            if (!SelectedTags.Contains(existing))
                SelectedTags.Add(existing);

            return true;
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
        /// 画像と同じディレクトリの gallery_edit_log.jsonl へ、タグ操作 1 件分のログを 1 行追記する。
        /// 書き込みに失敗した場合は握りつぶす（タグ本体の .txt 保存・captioning_config_result.json への
        /// 反映はすでに完了しているため、この記録ファイルへの反映失敗でタグ編集自体を失敗扱いにはしない）。
        /// </summary>
        private void LogEdit(string operation, IReadOnlyList<string> tags)
        {
            try
            {
                var directory = Path.GetDirectoryName(FullPath);
                if (string.IsNullOrEmpty(directory))
                    return;

                var entry = new GalleryEditLogEntry(DateTimeOffset.Now, FileName, operation, tags);
                var line = JsonSerializer.Serialize(entry, EditLogWriteOptions);
                File.AppendAllText(Path.Combine(directory, EditLogFileName), line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // 作業ログの記録失敗はタグ編集自体には影響させない
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
