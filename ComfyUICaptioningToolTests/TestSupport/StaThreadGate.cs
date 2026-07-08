namespace ComfyUICaptioningToolTests.TestSupport
{
    /// <summary>
    /// WPF の <c>FrameworkElement</c>（<c>SymbolIcon</c>・<c>NavigationViewItem</c> 等）を初めて生成する際の
    /// 内部初期化は、複数の xUnit テストクラスが並列に別々の STA スレッドから同時実行すると
    /// 不安定になることがある（"呼び出しスレッドは STA である必要があります" が STA スレッドからでも
    /// 稀に発生する）。テストクラス間でこのロックを共有し、STA スレッド上での WPF オブジェクト生成を
    /// プロセス全体で直列化することで回避する。
    /// </summary>
    internal static class StaThreadGate
    {
        public static readonly object Lock = new();
    }
}
