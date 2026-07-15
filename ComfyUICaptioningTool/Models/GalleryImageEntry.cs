using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace ComfyUICaptioningTool.Models
{
    /// <summary>
    /// GalleryPage の一覧表示用に、1 枚の画像とその同名 .txt から読み込んだタグ・サムネイルをまとめたもの。
    /// タグはカード上で追加・削除でき、変更のたびに同名 .txt へ即時保存する。
    /// </summary>
    public partial class GalleryImageEntry : ObservableObject
    {
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

        public GalleryImageEntry(string fileName, string fullPath, IEnumerable<string> tags, BitmapImage? thumbnail)
        {
            FileName = fileName;
            FullPath = fullPath;
            Thumbnail = thumbnail;
            Tags = new ObservableCollection<string>(tags);
            Tags.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasTags));
        }

        /// <summary>
        /// タグを追加する。前後の空白は trim し、大文字小文字を無視して既存タグと重複する場合・
        /// trim 後に空文字になる場合は追加しない。追加時は即座に同名 .txt へ保存する。
        /// </summary>
        public void AddTag(string tag)
        {
            var trimmed = tag.Trim();
            if (trimmed.Length == 0)
                return;
            if (Tags.Any(t => string.Equals(t, trimmed, StringComparison.OrdinalIgnoreCase)))
                return;

            Tags.Add(trimmed);
            SaveTags();
        }

        /// <summary>タグを削除する。削除できた場合は即座に同名 .txt へ保存する。</summary>
        [RelayCommand]
        public void RemoveTag(string tag)
        {
            if (Tags.Remove(tag))
                SaveTags();
        }

        /// <summary>カード上の「タグを追加」入力欄（<see cref="NewTagInput"/>）の内容をタグとして追加する。</summary>
        [RelayCommand]
        private void AddNewTag()
        {
            AddTag(NewTagInput);
            NewTagInput = "";
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
    }
}
