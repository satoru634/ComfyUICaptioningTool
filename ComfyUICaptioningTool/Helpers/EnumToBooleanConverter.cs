using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Appearance;

namespace ComfyUICaptioningTool.Helpers
{
    /// <summary>
    /// <see cref="ApplicationTheme"/> の値と、RadioButton 等の <c>IsChecked</c>（bool）を
    /// 相互変換する値コンバーター。<c>ConverterParameter</c> に列挙値の名前（文字列）を渡して使用する。
    /// </summary>
    internal class EnumToBooleanConverter : IValueConverter
    {
        /// <summary>
        /// 列挙値 (<paramref name="value"/>) が <paramref name="parameter"/> で指定した列挙値名と一致するかどうかを bool で返す。
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not String enumString)
            {
                throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName");
            }

            if (!Enum.IsDefined(typeof(ApplicationTheme), value))
            {
                throw new ArgumentException("ExceptionEnumToBooleanConverterValueMustBeAnEnum");
            }

            var enumValue = Enum.Parse(typeof(ApplicationTheme), enumString);

            return enumValue.Equals(value);
        }

        /// <summary>
        /// RadioButton がチェックされたときに、<paramref name="parameter"/> の列挙値名から対応する列挙値へ変換して返す。
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not String enumString)
            {
                throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName");
            }

            return Enum.Parse(typeof(ApplicationTheme), enumString);
        }
    }
}
