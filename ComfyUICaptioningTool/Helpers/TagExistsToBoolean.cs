using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ComfyUICaptioningTool.Helpers
{
    public class TagExistsToBoolean : IMultiValueConverter
    {
        /// <summary>
        /// タグが存在するかどうかを判定し、存在する場合はtrue、存在しない場合はfalseを返す。
        /// </summary>
        /// <param name="values">変換対象の値</param>
        /// <param name="targetType">変換後の型</param>
        /// <param name="parameter">変換パラメータ</param>
        /// <param name="culture">カルチャ情報</param>
        /// <returns>タグが存在する場合はtrue、存在しない場合はfalse</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 3)
            {
                return false;
            }

            // 0: string text, 1: List<string> tagList, 2: bool isInversion
            string? text = values[0] as string;
            IList<string>? tagList = values[1] as IList<string>;
            bool isInversion = values[2] as bool? ?? false;

            // textがnullまたは空文字の場合は、isInversionの値を反転して返す
            if (text == null || text == "")
            {
                return !isInversion;
            }

            // tagListがnullまたは空の場合は、isInversionの値を反転して返す
            if (tagList == null || tagList.Count == 0)
            {
                return !isInversion;
            }

            // タグが存在するかどうかを判定し、存在する場合はisInversionの値を反転して返す、
            // 存在しない場合はisInversionの値をそのまま返す
            // 大文字小文字は無視して比較する（AddTag/RemoveTag等、アプリ全体のタグ重複判定と合わせるため）
            if (tagList?.Contains(text, StringComparer.OrdinalIgnoreCase) == true)
            {
                return !isInversion;
            }

            return isInversion;
        }

        /// <summary>
        /// ConvertBackは未実装です。
        /// </summary>
        /// <param name="value">変換対象の値</param>
        /// <param name="targetType">変換後の型</param>
        /// <param name="parameter">変換パラメータ</param>
        /// <param name="culture">カルチャ情報</param>
        /// <returns>変換結果</returns>
        /// <exception cref="NotImplementedException"></exception>
        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
