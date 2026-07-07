using System.ComponentModel;
using System.Globalization;
using ComfyUICaptioningTool.Helpers;

namespace ComfyUICaptioningToolTests.Helpers
{
    /// <summary>
    /// <see cref="LocalizationManager"/> のカルチャ切替・キー解決・フォールバック挙動を検証するテスト。
    /// </summary>
    public class LocalizationManagerTests
    {
        /// <summary>
        /// LocalizationManager.Instance はプロセス全体で共有されるシングルトンのため、
        /// テスト間で状態が漏れないよう元のカルチャを保存・復元するヘルパー。
        /// </summary>
        private static void WithCulture(string cultureName, Action action)
        {
            var original = LocalizationManager.Instance.CurrentCulture;
            try
            {
                LocalizationManager.Instance.CurrentCulture = new CultureInfo(cultureName);
                action();
            }
            finally
            {
                LocalizationManager.Instance.CurrentCulture = original;
            }
        }

        /// <summary>カルチャが "ja" のとき、インデクサーが日本語のリソース文字列を返すこと。</summary>
        [Fact]
        public void Indexer_JapaneseCulture_ReturnsJapaneseText() =>
            WithCulture("ja", () =>
                Assert.Equal("設定", LocalizationManager.Instance["Settings_Title"]));

        /// <summary>カルチャが "en" のとき、インデクサーが英語のリソース文字列を返すこと。</summary>
        [Fact]
        public void Indexer_EnglishCulture_ReturnsEnglishText() =>
            WithCulture("en", () =>
                Assert.Equal("Settings", LocalizationManager.Instance["Settings_Title"]));

        /// <summary>"en-US" のような地域付きカルチャでも、"en" のサテライトリソースにフォールバックすること。</summary>
        [Fact]
        public void Indexer_EnglishUsCulture_FallsBackToEnglishSatellite() =>
            WithCulture("en-US", () =>
                Assert.Equal("Settings", LocalizationManager.Instance["Settings_Title"]));

        /// <summary>存在しないキーを指定した場合、キー文字列自体がそのまま返ること。</summary>
        [Fact]
        public void Indexer_UnknownKey_ReturnsKeyItself()
        {
            Assert.Equal("NonExistentKey", LocalizationManager.Instance["NonExistentKey"]);
        }

        /// <summary>CurrentCulture を設定すると、CultureInfo.CurrentUICulture にも反映されること。</summary>
        [Fact]
        public void CurrentCulture_Set_UpdatesCurrentUICulture()
        {
            var original = LocalizationManager.Instance.CurrentCulture;
            try
            {
                LocalizationManager.Instance.CurrentCulture = new CultureInfo("en");

                Assert.Equal("en", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
            }
            finally
            {
                LocalizationManager.Instance.CurrentCulture = original;
            }
        }

        /// <summary>異なる値へ CurrentCulture を変更すると、インデクサー用の "Item[]" 変更通知が発行されること。</summary>
        [Fact]
        public void CurrentCulture_SetDifferentValue_RaisesPropertyChangedForIndexer()
        {
            var original = LocalizationManager.Instance.CurrentCulture;
            try
            {
                LocalizationManager.Instance.CurrentCulture = new CultureInfo("ja");
                var changed = new List<string?>();
                ((INotifyPropertyChanged)LocalizationManager.Instance).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

                LocalizationManager.Instance.CurrentCulture = new CultureInfo("en");

                Assert.Contains("Item[]", changed);
            }
            finally
            {
                LocalizationManager.Instance.CurrentCulture = original;
            }
        }

        /// <summary>現在と同じ値を CurrentCulture に設定した場合は、変更通知が発行されないこと。</summary>
        [Fact]
        public void CurrentCulture_SetSameValue_DoesNotRaisePropertyChanged()
        {
            var original = LocalizationManager.Instance.CurrentCulture;
            try
            {
                LocalizationManager.Instance.CurrentCulture = new CultureInfo("ja");
                var changed = new List<string?>();
                ((INotifyPropertyChanged)LocalizationManager.Instance).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

                LocalizationManager.Instance.CurrentCulture = new CultureInfo("ja");

                Assert.Empty(changed);
            }
            finally
            {
                LocalizationManager.Instance.CurrentCulture = original;
            }
        }
    }
}
