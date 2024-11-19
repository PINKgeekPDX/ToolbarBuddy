using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using ToolbarApp.Services;
using ToolbarApp.Models;
using System.Text.Json;
using System.Threading.Tasks;

namespace ToolbarApp.Views
{
    public partial class ToolbarWindow : Window
    {
        private readonly ConfigurationService _configService;
        private readonly LoggingService _loggingService;
        private readonly PluginService _pluginService;

        public ToolbarWindow(ConfigurationService configService, LoggingService loggingService, PluginService pluginService)
        {
            InitializeComponent();
            _configService = configService;
            _loggingService = loggingService;
            _pluginService = pluginService;

            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                await webView.EnsureCoreWebView2Async(null);
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;

                // Expose backend services to JavaScript
                webView.CoreWebView2.AddHostObjectToScript("scriptExecutor", new ScriptExecutorJs(new ScriptExecutor(_loggingService), _loggingService));
                webView.CoreWebView2.AddHostObjectToScript("systemService", new SystemServiceJs(new SystemService(_loggingService), _loggingService));
                webView.CoreWebView2.AddHostObjectToScript("pluginService", new PluginServiceJs(_pluginService, _loggingService));

                webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;

                LoadHtmlContent();
                await LoadToolbarConfigurationAsync();

                _loggingService.Log("Toolbar WebView initialized successfully.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error initializing Toolbar WebView.", ex);
                MessageBox.Show("Error initializing Toolbar WebView. Please check the log file for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadHtmlContent()
        {
            // Load the HTML content into WebView2. Ensure that the HTML, CSS, and JS files are correctly placed in wwwroot.
            string htmlContent = @"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <title>Toolbar</title>
    <link rel='stylesheet' href='styles.css'>
</head>
<body>
    <div id='toolbar' class='toolbar'></div>
    <div id='terminal' class='terminal'></div>
    <script src='main.js'></script>
    <script src='Toolbar.js'></script>
</body>
</html>";
            webView.CoreWebView2.NavigateToString(htmlContent);
        }

        private async Task LoadToolbarConfigurationAsync()
        {
            try
            {
                var config = await _configService.LoadConfigurationAsync();
                if (config.Buttons == null || config.Buttons.Count == 0)
                {
                    AddSampleButtons();
                }
                else
                {
                    foreach (var button in config.Buttons)
                    {
                        await AddButtonToToolbarAsync(button);
                    }
                }
                _loggingService.Log("Toolbar configuration loaded successfully.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error loading toolbar configuration.", ex);
                MessageBox.Show("Error loading toolbar configuration. Please check the log file for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task AddButtonToToolbarAsync(ButtonConfig button)
        {
            try
            {
                string json = JsonSerializer.Serialize(button);
                await webView.CoreWebView2.ExecuteScriptAsync($"toolbar.addButton({json});");
                _loggingService.Log($"Button added to toolbar: {button.Label}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error adding button to toolbar: {button.Label}", ex);
                throw;
            }
        }

        private void AddSampleButtons()
        {
            try
            {
                var sampleButtons = new List<ButtonConfig>
                {
                    new ButtonConfig
                    {
                        Label = "PowerShell Test",
                        Type = "script",
                        Config = new Dictionary<string, object>
                        {
                            { "scriptType", "PowerShell" },
                            { "command", "Get-Process | Select-Object -First 5" },
                            { "adminRights", false }
                        }
                    },
                    new ButtonConfig
                    {
                        Label = "Open Notepad",
                        Type = "application",
                        Config = new Dictionary<string, object>
                        {
                            { "path", "notepad.exe" },
                            { "arguments", "" },
                            { "adminRights", false }
                        }
                    },
                    new ButtonConfig
                    {
                        Label = "Google",
                        Type = "url",
                        Config = new Dictionary<string, object>
                        {
                            { "url", "https://www.google.com" }
                        }
                    }
                };

                var settingsTask = _configService.LoadConfigurationAsync();
                settingsTask.Wait();
                var settings = settingsTask.Result;
                settings.ToolbarConfig.Buttons.AddRange(sampleButtons);
                _configService.SaveConfigurationAsync(settings).Wait();

                foreach (var button in sampleButtons)
                {
                    AddButtonToToolbarAsync(button).Wait();
                }
                _loggingService.Log("Sample buttons added successfully.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error adding sample buttons.", ex);
                MessageBox.Show("Error adding sample buttons. Please check the log file for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = JsonSerializer.Deserialize<Dictionary<string, object>>(e.WebMessageAsJson);
                string type = message["type"].ToString();

                switch (type)
                {
                    case "saveButtonOrder":
                        var order = JsonSerializer.Deserialize<List<string>>(message["order"].ToString());
                        await SaveButtonOrderAsync(order);
                        break;
                    case "openConfigDialog":
                        var buttonJson = message["button"].ToString();
                        var button = JsonSerializer.Deserialize<ButtonConfig>(buttonJson);
                        OpenConfigDialog(button);
                        break;
                    case "openSettings":
                        OpenSettingsDialog();
                        break;
                    default:
                        _loggingService.Log($"Unknown message type received: {type}", LogLevel.Warning);
                        break;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error processing WebView message.", ex);
            }
        }

        private async Task SaveButtonOrderAsync(List<string> order)
        {
            try
            {
                var settings = await _configService.LoadConfigurationAsync();
                var orderedButtons = order.Select(id => settings.ToolbarConfig.Buttons.FirstOrDefault(b => b.Id == id)).Where(b => b != null).ToList();
                settings.ToolbarConfig.Buttons = orderedButtons;
                await _configService.SaveConfigurationAsync(settings);
                _loggingService.Log("Button order saved successfully.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error saving button order.", ex);
                MessageBox.Show("Error saving button order. Please check the log file for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenConfigDialog(ButtonConfig button)
        {
            try
            {
                var configWindow = new ButtonConfigWindow(button);
                configWindow.Owner = this;
                if (configWindow.ShowDialog() == true)
                {
                    // Update the button configuration in the toolbar
                    string json = JsonSerializer.Serialize(button);
                    webView.CoreWebView2.ExecuteScriptAsync($"toolbar.updateButton({json});").Wait();

                    // Save the updated configuration
                    var settingsTask = _configService.LoadConfigurationAsync();
                    settingsTask.Wait();
                    var settings = settingsTask.Result;
                    var index = settings.ToolbarConfig.Buttons.FindIndex(b => b.Id == button.Id);
                    if (index != -1)
                    {
                        settings.ToolbarConfig.Buttons[index] = button;
                        _configService.SaveConfigurationAsync(settings).Wait();
                    }
                    _loggingService.Log($"Button configuration updated: {button.Label}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error updating button configuration: {button.Label}", ex);
                MessageBox.Show("Error updating button configuration. Please check the log file for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ToggleToolbarVisibility()
        {
            this.Visibility = this.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
            _loggingService.Log($"Toolbar visibility toggled to: {this.Visibility}");
        }
    }
}
