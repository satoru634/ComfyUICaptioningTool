using System.Globalization;
using ComfyUICaptioningTool.Helpers;

namespace ComfyUICaptioningToolTests.Helpers
{
    /// <summary>
    /// <see cref="ObjectEqualsConverter"/> の変換ロジックを検証するテスト。
    /// </summary>
    public class ObjectEqualsConverterTests
    {
        private readonly ObjectEqualsConverter _converter = new();

        private static readonly Type BoolType = typeof(bool);

        /// <summary>values が null の場合は false を返すこと。</summary>
        [Fact]
        public void Convert_ValuesNull_ReturnsFalse()
        {
            var result = _converter.Convert(null!, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(false, result);
        }

        /// <summary>values の要素数が2でない場合は false を返すこと。</summary>
        [Fact]
        public void Convert_ValuesLengthNot2_ReturnsFalse()
        {
            var result = _converter.Convert(new object[] { "a" }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(false, result);
        }

        /// <summary>両方の値が同一インスタンスの場合は true を返すこと。</summary>
        [Fact]
        public void Convert_SameInstance_ReturnsTrue()
        {
            var instance = new object();

            var result = _converter.Convert(new[] { instance, instance }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(true, result);
        }

        /// <summary>異なるインスタンスの場合は false を返すこと。</summary>
        [Fact]
        public void Convert_DifferentInstances_ReturnsFalse()
        {
            var result = _converter.Convert(new[] { new object(), new object() }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(false, result);
        }

        /// <summary>両方が null の場合は true を返すこと。</summary>
        [Fact]
        public void Convert_BothNull_ReturnsTrue()
        {
            var result = _converter.Convert(new object[] { null!, null! }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(true, result);
        }

        /// <summary>片方のみ null の場合は false を返すこと。</summary>
        [Fact]
        public void Convert_OneNull_ReturnsFalse()
        {
            var result = _converter.Convert(new object[] { new object(), null! }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(false, result);
        }

        /// <summary>ConvertBack は未実装であること。</summary>
        [Fact]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() =>
                _converter.ConvertBack(true, new[] { typeof(object) }, null!, CultureInfo.InvariantCulture));
        }
    }
}
