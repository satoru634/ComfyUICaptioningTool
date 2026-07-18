using System.Collections.Generic;
using System.Globalization;
using ComfyUICaptioningTool.Helpers;

namespace ComfyUICaptioningToolTests.Helpers
{
    /// <summary>
    /// <see cref="TagExistsToBoolean"/> の変換ロジックを検証するテスト。
    /// </summary>
    public class TagExistsToBooleanTests
    {
        private readonly TagExistsToBoolean _converter = new();

        private static readonly Type BoolType = typeof(bool);

        /// <summary>values が null の場合は false を返すこと。</summary>
        [Fact]
        public void Convert_ValuesNull_ReturnsFalse()
        {
            var result = _converter.Convert(null!, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(false, result);
        }

        /// <summary>values の要素数が3でない場合は false を返すこと。</summary>
        [Fact]
        public void Convert_ValuesLengthNot3_ReturnsFalse()
        {
            var result = _converter.Convert(new object[] { "cat", new List<string> { "cat" } }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(false, result);
        }

        /// <summary>text が空文字で isInversion=false の場合は true を返すこと。</summary>
        [Fact]
        public void Convert_TextEmpty_IsInversionFalse_ReturnsTrue()
        {
            var result = _converter.Convert(new object[] { "", new List<string> { "cat" }, false }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(true, result);
        }

        /// <summary>text が空文字で isInversion=true の場合は false を返すこと。</summary>
        [Fact]
        public void Convert_TextEmpty_IsInversionTrue_ReturnsFalse()
        {
            var result = _converter.Convert(new object[] { "", new List<string> { "cat" }, true }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(false, result);
        }

        /// <summary>tagList が null で isInversion=false の場合は true を返すこと。</summary>
        [Fact]
        public void Convert_TagListNull_IsInversionFalse_ReturnsTrue()
        {
            var result = _converter.Convert(new object[] { "cat", null!, false }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(true, result);
        }

        /// <summary>tagList が空で isInversion=true の場合は false を返すこと。</summary>
        [Fact]
        public void Convert_TagListEmpty_IsInversionTrue_ReturnsFalse()
        {
            var result = _converter.Convert(new object[] { "cat", new List<string>(), true }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(false, result);
        }

        /// <summary>タグが存在し isInversion=false（存在確認）の場合は true を返すこと。</summary>
        [Fact]
        public void Convert_TagExists_IsInversionFalse_ReturnsTrue()
        {
            var result = _converter.Convert(new object[] { "cat", new List<string> { "dog", "cat" }, false }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(true, result);
        }

        /// <summary>タグが存在せず isInversion=false（存在確認）の場合は false を返すこと。</summary>
        [Fact]
        public void Convert_TagNotExists_IsInversionFalse_ReturnsFalse()
        {
            var result = _converter.Convert(new object[] { "cat", new List<string> { "dog" }, false }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(false, result);
        }

        /// <summary>タグが存在し isInversion=true（未存在確認）の場合は false を返すこと。</summary>
        [Fact]
        public void Convert_TagExists_IsInversionTrue_ReturnsFalse()
        {
            var result = _converter.Convert(new object[] { "cat", new List<string> { "cat" }, true }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(false, result);
        }

        /// <summary>タグが存在せず isInversion=true（未存在確認）の場合は true を返すこと。</summary>
        [Fact]
        public void Convert_TagNotExists_IsInversionTrue_ReturnsTrue()
        {
            var result = _converter.Convert(new object[] { "cat", new List<string> { "dog" }, true }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(true, result);
        }

        /// <summary>大文字小文字が異なっていても同一タグとして扱われること（アプリ全体のタグ重複判定との整合性）。</summary>
        [Fact]
        public void Convert_TagExistsWithDifferentCase_IsInversionFalse_ReturnsTrue()
        {
            var result = _converter.Convert(new object[] { "Cat", new List<string> { "cat" }, false }, BoolType, null!, CultureInfo.InvariantCulture);

            Assert.Equal(true, result);
        }

        /// <summary>ConvertBack は未実装のため NotImplementedException を送出すること。</summary>
        [Fact]
        public void ConvertBack_Always_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() =>
                _converter.ConvertBack(true, new[] { typeof(string) }, null!, CultureInfo.InvariantCulture));
        }
    }
}
