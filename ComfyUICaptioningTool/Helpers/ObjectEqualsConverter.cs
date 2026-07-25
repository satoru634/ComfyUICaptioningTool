using System;
using System.Globalization;
using System.Windows.Data;

namespace ComfyUICaptioningTool.Helpers
{
    /// <summary>
    /// 2つの値が等しいかどうかだけを判定するコンバーター。GalleryPage の画像タイル選択状態
    /// （ToggleButton.IsChecked、タイル自身と GalleryViewModel.SelectedImage を比較）の判定に使う。
    /// </summary>
    public class ObjectEqualsConverter : IMultiValueConverter
    {
        /// <summary>0: 比較対象1、1: 比較対象2。両方が等しい（Equals）場合のみ true を返す。</summary>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2)
                return false;

            return Equals(values[0], values[1]);
        }

        /// <summary>ConvertBackは未実装です。</summary>
        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
