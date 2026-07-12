using System.Globalization;
using System.Windows;
using ComfyUICaptioningTool.Helpers;

namespace ComfyUICaptioningToolTests.Helpers
{
    /// <summary>
    /// <see cref="StringToVisibilityConverter"/> の変換ロジックを検証するテスト。
    /// </summary>
    public class StringToVisibilityConverterTests
    {
        private readonly StringToVisibilityConverter _converter = new();

        /// <summary>空文字でない文字列は Visible に変換されること。</summary>
        [Fact]
        public void Convert_NonEmptyString_ReturnsVisible()
        {
            var result = _converter.Convert("some text", typeof(Visibility), null!, CultureInfo.InvariantCulture);

            Assert.Equal(Visibility.Visible, result);
        }

        /// <summary>空文字は Collapsed に変換されること。</summary>
        [Fact]
        public void Convert_EmptyString_ReturnsCollapsed()
        {
            var result = _converter.Convert("", typeof(Visibility), null!, CultureInfo.InvariantCulture);

            Assert.Equal(Visibility.Collapsed, result);
        }

        /// <summary>null は Collapsed に変換されること。</summary>
        [Fact]
        public void Convert_Null_ReturnsCollapsed()
        {
            var result = _converter.Convert(null!, typeof(Visibility), null!, CultureInfo.InvariantCulture);

            Assert.Equal(Visibility.Collapsed, result);
        }

        /// <summary>文字列以外の値は Collapsed に変換されること。</summary>
        [Fact]
        public void Convert_NonStringValue_ReturnsCollapsed()
        {
            var result = _converter.Convert(123, typeof(Visibility), null!, CultureInfo.InvariantCulture);

            Assert.Equal(Visibility.Collapsed, result);
        }

        /// <summary>ConvertBack は未実装のため NotImplementedException を送出すること。</summary>
        [Fact]
        public void ConvertBack_Always_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() =>
                _converter.ConvertBack(Visibility.Visible, typeof(string), null!, CultureInfo.InvariantCulture));
        }
    }
}
