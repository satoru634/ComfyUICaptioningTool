using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Threading;

namespace ComfyUICaptioningToolTests.TestSupport
{
    /// <summary>
    /// WPF の <c>FrameworkElement</c> を生成する非同期 ViewModel メソッドを STA スレッドで実行するヘルパー。
    /// 素の <see cref="Thread"/> には <see cref="SynchronizationContext"/> が存在しないため、
    /// 実際の非同期 I/O（<c>File.ReadAllLinesAsync</c> 等、同期完了しない await）を含むコードを
    /// 単純に <c>GetAwaiter().GetResult()</c> で待つと、継続がスレッドプール（MTA）へ流れてしまい、
    /// その後の WPF オブジェクト生成が "STA が必要" 例外で失敗する。
    /// <see cref="DispatcherSynchronizationContext"/> と <see cref="Dispatcher.PushFrame"/> による
    /// メッセージポンプで、継続を常にこの STA スレッドへ戻す。
    /// </summary>
    internal static class StaTestRunner
    {
        public static void Run(Func<Task> asyncAction)
        {
            lock (StaThreadGate.Lock)
            {
                Exception? caught = null;
                var thread = new Thread(() =>
                {
                    try
                    {
                        var dispatcher = Dispatcher.CurrentDispatcher;
                        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));

                        var task = asyncAction();
                        var frame = new DispatcherFrame();
                        task.ContinueWith(
                            _ => frame.Continue = false,
                            TaskScheduler.FromCurrentSynchronizationContext());
                        Dispatcher.PushFrame(frame);

                        // 完了済みタスクを待機し、発生していた例外を再スローさせる
                        task.GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        caught = ex;
                    }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
                if (caught is not null)
                    ExceptionDispatchInfo.Capture(caught).Throw();
            }
        }
    }
}
