using System.Collections.Generic;
using System.Globalization;
using ComfyUICaptioningTool.Helpers;

namespace ComfyUICaptioningToolTests.Helpers
{
    /// <summary>
    /// <see cref="TagInCollectionConverter"/> の変換ロジックを検証するテスト。
    /// </summary>
    public class TagInCollectionConverterTests
    {
        private readonly TagInCollectionConverter _converter = new();

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
            var result = _converter.Convert(new object[] { "cat" }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(false, result);
        }

        /// <summary>tagList が空の場合、tag の内容に関わらず false を返すこと（初期状態で全て未選択になることの検証）。</summary>
        [Fact]
        public void Convert_TagListEmpty_ReturnsFalse()
        {
            var result = _converter.Convert(new object[] { "cat", new List<string>() }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(false, result);
        }

        /// <summary>tagList が null の場合は false を返すこと。</summary>
        [Fact]
        public void Convert_TagListNull_ReturnsFalse()
        {
            var result = _converter.Convert(new object[] { "cat", null! }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(false, result);
        }

        /// <summary>tag が空文字の場合は false を返すこと。</summary>
        [Fact]
        public void Convert_TagEmpty_ReturnsFalse()
        {
            var result = _converter.Convert(new object[] { "", new List<string> { "cat" } }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(false, result);
        }

        /// <summary>tagList に tag が含まれる場合は true を返すこと。</summary>
        [Fact]
        public void Convert_TagInList_ReturnsTrue()
        {
            var result = _converter.Convert(new object[] { "cat", new List<string> { "cat", "dog" } }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(true, result);
        }

        /// <summary>tagList に tag が含まれない場合は false を返すこと。</summary>
        [Fact]
        public void Convert_TagNotInList_ReturnsFalse()
        {
            var result = _converter.Convert(new object[] { "cat", new List<string> { "dog" } }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(false, result);
        }

        /// <summary>大文字小文字を無視して比較すること。</summary>
        [Fact]
        public void Convert_TagInListIgnoringCase_ReturnsTrue()
        {
            var result = _converter.Convert(new object[] { "Cat", new List<string> { "cat" } }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(true, result);
        }

        /// <summary>ConvertBack は未実装であること。</summary>
        [Fact]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() =>
                _converter.ConvertBack(true, new[] { typeof(string) }, null!, CultureInfo.InvariantCulture));
        }
    }
}
