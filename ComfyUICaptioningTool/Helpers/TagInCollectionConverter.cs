using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace ComfyUICaptioningTool.Helpers
{
    /// <summary>
    /// 指定したタグ文字列がコレクションに含まれるかどうかだけを判定するコンバーター。
    /// TagExistsToBoolean は対象リストが空の場合に isInversion を反転した値を返す
    /// （「タグ未入力時はボタンを活性化する」用途向けの挙動）ため、GalleryPage のタグ選択
    /// トグルボタン（SelectedTags が空＝すべて未選択であるべき）には適さない。
    /// </summary>
    public class TagInCollectionConverter : IMultiValueConverter
    {
        /// <summary>
        /// 0: string tag, 1: IList&lt;string&gt; tagList。tagList に tag が含まれる場合のみ true を返す。
        /// 大文字小文字は無視して比較する（アプリ全体のタグ重複判定と合わせるため）。
        /// </summary>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2)
                return false;

            var tag = values[0] as string;
            var tagList = values[1] as IList<string>;

            if (string.IsNullOrEmpty(tag) || tagList == null)
                return false;

            return tagList.Contains(tag, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>ConvertBackは未実装です。</summary>
        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
