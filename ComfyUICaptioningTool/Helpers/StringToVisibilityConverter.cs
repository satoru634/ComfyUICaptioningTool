using System.Globalization;
using System.Windows.Data;

namespace ComfyUICaptioningTool.Helpers
{
    public class StringToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 文字列が空文字でない場合は Visible を返し、それ以外の場合は Collapsed を返す。
        /// </summary>
        /// <param name="value">変換対象の値</param>
        /// <param name="targetType">変換後の型</param>
        /// <param name="parameter">変換パラメータ</param>
        /// <param name="culture">カルチャ情報</param>
        /// <returns>変換結果</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                if (text != "")
                {
                    return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }

        /// <summary>
        /// 逆変換は未実装。
        /// </summary>
        /// <param name="value">変換元の値。</param>
        /// <param name="targetType">変換先の型。</param>
        /// <param name="parameter">変換パラメーター。</param>
        /// <param name="culture">カルチャ情報。</param>
        /// <returns>変換結果</returns>
        /// <exception cref="NotImplementedException"></exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
