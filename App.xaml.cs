using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ToolBarApp.Services;
using ToolBarApp.Views;

namespace ToolBarApp
{
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;

        public App()
        {
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Configures services and registers them with the DI container.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        private void ConfigureServices(ServiceCollection services)
        {
            // Add logging
            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.AddDebug();
                // Add other logging providers if needed
            });

            // Register services
            services.AddSingleton<ConfigurationService>();
            services.AddSingleton<ScriptExecutor>();
            services.AddSingleton<SystemService>();
            services.AddSingleton<PluginService>();
            services.AddSingleton<ToolbarService>();
            services.AddSingleton<TerminalService>(); // Register TerminalService

            // Register windows
            services.AddTransient<MainWindow>();
            services.AddTransient<TerminalWindow>();
            services.AddTransient<ButtonConfigWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var mainWindow = _serviceProvider.GetService<MainWindow>();
            mainWindow.Show();
            base.OnStartup(e);
        }
    }
}
