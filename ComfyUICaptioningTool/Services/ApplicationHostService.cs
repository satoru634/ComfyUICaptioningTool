using System.Globalization;
using ComfyUICaptioningTool.Helpers;
using ComfyUICaptioningTool.Models;
using ComfyUICaptioningTool.Views.Pages;
using ComfyUICaptioningTool.Views.Windows;
using ComfyUILibs.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wpf.Ui;

namespace ComfyUICaptioningTool.Services
{
    /// <summary>
    /// Managed host of the application.
    /// </summary>
    public class ApplicationHostService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Setting<AppConfig> _config;

        private INavigationWindow _navigationWindow;

        public ApplicationHostService(IServiceProvider serviceProvider, Setting<AppConfig> config)
        {
            _serviceProvider = serviceProvider;
            _config = config;
        }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            ApplyCulture();
            await HandleActivationAsync();
        }

        /// <summary>
        /// 保存済みの Config.Data.Language から表示言語を適用する。
        /// OS ロケールに関わらず、既定値（"ja"）に固定するため起動時に明示的に設定する。
        /// </summary>
        private void ApplyCulture()
        {
            var culture = new CultureInfo(_config.Data.Language);
            LocalizationManager.Instance.CurrentCulture = culture;
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Creates main window during activation.
        /// </summary>
        private async Task HandleActivationAsync()
        {
            if (!Application.Current.Windows.OfType<MainWindow>().Any())
            {
                _navigationWindow = (
                    _serviceProvider.GetService(typeof(INavigationWindow)) as INavigationWindow
                )!;
                _navigationWindow!.ShowWindow();

                _navigationWindow.Navigate(typeof(Views.Pages.MainPage));
            }

            await Task.CompletedTask;
        }
    }
}
