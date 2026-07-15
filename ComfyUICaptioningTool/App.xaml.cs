using ComfyUICaptioningTool.Models;
using ComfyUICaptioningTool.Services;
using ComfyUICaptioningTool.ViewModels.Pages;
using ComfyUICaptioningTool.ViewModels.Windows;
using ComfyUICaptioningTool.Views.Pages;
using ComfyUICaptioningTool.Views.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Reflection;
using System.Windows.Threading;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;
using ComfyUILibs.Common;

namespace ComfyUICaptioningTool
{
    /// <summary>
    /// アプリケーションのエントリポイント。
    /// .NET Generic Host を用いて DI コンテナを構築し、ホストのライフサイクルを管理する。
    /// </summary>
    public partial class App
    {
        // .NET Generic Host: DI・設定・ログ・ライフサイクル管理を一元提供する仕組み
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(AppContext.BaseDirectory)); })
            .ConfigureServices((context, services) =>
            {
                // 設定ファイルを実行ディレクトリ直下の JSON に永続化するシングルトン
                var setting = new Setting<AppConfig>(Path.GetFullPath("ComfyUICaptioningTool_setting.json"));
                services.AddNavigationViewPageProvider();

                // アプリ起動時にメインウィンドウを表示するホステッドサービス
                services.AddHostedService<ApplicationHostService>();

                services.AddSingleton(setting);

                // テーマ（ライト／ダーク）切り替えサービス
                services.AddSingleton<IThemeService, ThemeService>();

                // タスクバー操作サービス
                services.AddSingleton<ITaskBarService, TaskBarService>();

                // スナックバー通知サービス
                services.AddSingleton<ISnackbarService, SnackbarService>();

                // ナビゲーションサービス（ウィンドウを持たない画面遷移制御）
                services.AddSingleton<INavigationService, NavigationService>();

                // メインウィンドウ（ナビゲーションホスト）
                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                // 各ページと ViewModel
                services.AddSingleton<MainPage>();
                services.AddSingleton<MainPageViewModel>();
                services.AddSingleton<DataPage>();
                services.AddSingleton<DataViewModel>();
                services.AddSingleton<GalleryPage>();
                services.AddSingleton<GalleryViewModel>();
                services.AddSingleton<ReportPage>();
                services.AddSingleton<ReportViewModel>();
                services.AddSingleton<ConfigPage>();
                services.AddSingleton<ConfigViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();
            }).Build();

        /// <summary>
        /// DI コンテナのサービスプロバイダーを取得する。
        /// </summary>
        public static IServiceProvider Services
        {
            get { return _host.Services; }
        }

        /// <summary>
        /// アプリケーション起動時に呼び出される。ホストを開始してサービスを起動する。
        /// </summary>
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            await _host.StartAsync();
        }

        /// <summary>
        /// アプリケーション終了時に呼び出される。ホストをグレースフルシャットダウンして破棄する。
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();

            _host.Dispose();
        }

        /// <summary>
        /// ハンドルされない例外が発生したときに呼び出される。
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // 必要に応じてここでエラーダイアログ表示やログ記録を行う
        }
    }
}
