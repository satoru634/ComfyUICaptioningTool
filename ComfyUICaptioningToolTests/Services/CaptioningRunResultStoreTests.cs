using System.ComponentModel;
using ComfyUICaptioningTool.Models;
using ComfyUICaptioningTool.Services;

namespace ComfyUICaptioningToolTests.Services
{
    public class CaptioningRunResultStoreTests
    {
        [Fact]
        public void LastResult_InitialValue_IsNull()
        {
            var store = new CaptioningRunResultStore();

            Assert.Null(store.LastResult);
        }

        [Fact]
        public void LastResult_Set_ReflectsAssignedValue()
        {
            var store = new CaptioningRunResultStore();
            var result = new CaptioningRunResult(DateTime.Now, @"C:\images", true, 5, 1, 0, new List<string>());

            store.LastResult = result;

            Assert.Same(result, store.LastResult);
        }

        [Fact]
        public void LastResult_Set_RaisesPropertyChanged()
        {
            var store = new CaptioningRunResultStore();
            var changed = new List<string?>();
            ((INotifyPropertyChanged)store).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            store.LastResult = new CaptioningRunResult(DateTime.Now, @"C:\images", false, 1, 0, 0, new List<string>());

            Assert.Contains("LastResult", changed);
        }
    }
}
