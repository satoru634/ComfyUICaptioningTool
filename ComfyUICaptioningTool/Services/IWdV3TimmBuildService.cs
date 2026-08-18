namespace ComfyUICaptioningTool.Services
{
    /// <summary>
    /// wdv3-timm（ローカルプロセス版タグ付け）の実行環境（.venv・wdv3_timm.exe）を
    /// アプリと同階層の wdv3-timm フォルダに構築するサービス。
    /// </summary>
    public interface IWdV3TimmBuildService
    {
        /// <summary>wdv3_timm.exe が固定パスに存在し、常駐サーバーモードを起動できる状態かどうか。</summary>
        bool IsExeReady { get; }

        /// <summary>
        /// wdv3-timm フォルダ同梱の setup.bat（.venv 作成・依存関係インストール）→
        /// build_exe.bat（wdv3_timm.exe のビルド）を順に実行する。各スクリプトの標準出力・標準エラー出力は
        /// 1 行ごとに <paramref name="outputProgress"/> へ通知する。
        /// </summary>
        /// <param name="outputProgress">実行中の出力を1行ずつ受け取るコールバック。</param>
        /// <param name="cancellationToken">キャンセル用トークン。</param>
        /// <returns>setup.bat・build_exe.bat のいずれも終了コード 0 で完了した場合は true。</returns>
        Task<bool> BuildAsync(IProgress<string> outputProgress, CancellationToken cancellationToken = default);
    }
}
