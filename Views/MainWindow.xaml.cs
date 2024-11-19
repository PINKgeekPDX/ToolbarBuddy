// Views/MainWindow.xaml.cs
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms; // Ensure you have added a reference to System.Windows.Forms
using Microsoft.Web.WebView2.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ToolBarApp.Models;
using ToolBarApp.Services;

namespace ToolBarApp.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ToolbarService _toolbarService;
        private readonly ILogger<MainWindow> _logger;
        private readonly ConfigurationService _configService;
        private readonly TerminalService _terminalService;
        private NotifyIcon _trayIcon;

        public MainWindow(ToolbarService toolbarService, ILogger<MainWindow> logger, ConfigurationService configService, TerminalService terminalService)
        {
            InitializeComponent();
            _toolbarService = toolbarService ?? throw new ArgumentNullException(nameof(toolbarService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _terminalService = terminalService ?? throw new ArgumentNullException(nameof(terminalService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));

            InitializeWebView();
            InitializeTrayIcon();
            this.Closed += MainWindow_Closed;
        }

        /// <summary>
        /// Initializes the WebView2 control and sets up host objects.
        /// </summary>
        private async void InitializeWebView()
        {
            try
            {
                await webView.EnsureCoreWebView2Async(null);
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;

                // Register host objects to be accessible from JavaScript
                webView.CoreWebView2.AddHostObjectToScript("scriptExecutor", new ScriptExecutorJs(_toolbarService, _logger));
                webView.CoreWebView2.AddHostObjectToScript("systemService", new SystemServiceJs(_toolbarService, _logger));
                webView.CoreWebView2.AddHostObjectToScript("pluginService", new PluginServiceJs(_toolbarService, _logger));

                // Handle messages from JavaScript
                webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;

                // Load the initial HTML content
                LoadHtmlContent();

                // Load toolbar configuration and render buttons
                await LoadAndRenderToolbarAsync();

                _logger.LogInformation("WebView2 initialized successfully.");

                // Log the initialization to the terminal
                await _terminalService.LogAsync("WebView2 initialized successfully.", LogLevel.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing WebView2.");
                System.Windows.MessageBox.Show("Failed to initialize the web view. Please check the logs for more details.", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                await _terminalService.LogAsync($"Error initializing WebView2: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Loads the HTML content into WebView2.
        /// </summary>
        private void LoadHtmlContent()
        {
            try
            {
                string appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string htmlFilePath = Path.Combine(appDirectory, "wwwroot", "index.html");

                if (File.Exists(htmlFilePath))
                {
                    webView.CoreWebView2.Navigate(new Uri(htmlFilePath).AbsoluteUri);
                    _logger.LogInformation($"Loaded HTML content from {htmlFilePath}.");
                    _terminalService.LogAsync($"Loaded HTML content from {htmlFilePath}.", LogLevel.Information).Wait();
                }
                else
                {
                    _logger.LogError($"HTML file not found at path: {htmlFilePath}");
                    System.Windows.MessageBox.Show("HTML file not found. Please ensure all frontend files are present.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    _terminalService.LogAsync($"HTML file not found at path: {htmlFilePath}.", LogLevel.Error).Wait();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading HTML content.");
                System.Windows.MessageBox.Show("Failed to load HTML content. Please check the logs for more details.", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _terminalService.LogAsync($"Error loading HTML content: {ex.Message}", LogLevel.Error).Wait();
            }
        }

        /// <summary>
        /// Loads the toolbar configuration and renders buttons.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task LoadAndRenderToolbarAsync()
        {
            try
            {
                var config = await _toolbarService.LoadToolbarConfigAsync();

                if (config.Toolbars != null && config.Toolbars.Count > 0)
                {
                    foreach (var toolbar in config.Toolbars)
                    {
                        foreach (var button in toolbar.Buttons)
                        {
                            // Serialize button to JSON and send to frontend to add the button
                            string buttonJson = JsonSerializer.Serialize(button);
                            await webView.CoreWebView2.ExecuteScriptAsync($"toolbar.addButton({buttonJson});");
                        }
                    }

                    _logger.LogInformation("Toolbar buttons loaded and rendered successfully.");
                    await _terminalService.LogAsync("Toolbar buttons loaded and rendered successfully.", LogLevel.Information);
                }
                else
                {
                    _logger.LogWarning("No toolbar configurations found. Initializing with sample buttons.");
                    await _terminalService.LogAsync("No toolbar configurations found. Initializing with sample buttons.", LogLevel.Warning);
                    InitializeSampleButtons();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading and rendering toolbar.");
                System.Windows.MessageBox.Show("Failed to load toolbar configuration. Please check the logs for more details.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                await _terminalService.LogAsync($"Error loading and rendering toolbar: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Initializes sample buttons if no configuration is found.
        /// </summary>
        private void InitializeSampleButtons()
        {
            var sampleButtons = new List<ButtonConfig>
            {
                new ButtonConfig
                {
                    Id = "btn1",
                    Label = "PowerShell Test",
                    Type = "script",
                    Config = new Dictionary<string, object>
                    {
                        { "scriptType", "PowerShell" },
                        { "command", "Get-Process | Select-Object -First 5" },
                        { "adminRights", false }
                    },
                    Tooltip = "Execute a PowerShell script"
                },
                new ButtonConfig
                {
                    Id = "btn2",
                    Label = "Open Notepad",
                    Type = "application",
                    Config = new Dictionary<string, object>
                    {
                        { "path", "notepad.exe" },
                        { "arguments", "" },
                        { "adminRights", false }
                    },
                    Tooltip = "Launch Notepad"
                },
                new ButtonConfig
                {
                    Id = "btn3",
                    Label = "Google",
                    Type = "url",
                    Config = new Dictionary<string, object>
                    {
                        { "url", "https://www.google.com" }
                    },
                    Tooltip = "Open Google in default browser"
                }
            };

            // Add sample buttons via the toolbar service
            foreach (var button in sampleButtons)
            {
                _toolbarService.AddButtonAsync(button).Wait();
                _logger.LogInformation($"Sample button '{button.Label}' added.");
                _terminalService.LogAsync($"Sample button '{button.Label}' added.", LogLevel.Information).Wait();
            }

            _logger.LogInformation("Sample buttons initialized.");
            _terminalService.LogAsync("Sample buttons initialized.", LogLevel.Information).Wait();
        }

        /// <summary>
        /// Handles messages received from JavaScript.
        /// </summary>
        private async void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = JsonSerializer.Deserialize<Dictionary<string, object>>(e.WebMessageAsJson);
                if (message == null || !message.ContainsKey("type"))
                {
                    _logger.LogWarning("Received invalid message from frontend.");
                    await _terminalService.LogAsync("Received invalid message from frontend.", LogLevel.Warning);
                    return;
                }

                string messageType = message["type"].ToString();

                switch (messageType)
                {
                    case "saveButtonOrder":
                        if (message.ContainsKey("order"))
                        {
                            var order = JsonSerializer.Deserialize<List<string>>(message["order"].ToString());
                            await _toolbarService.ReorderButtonsAsync(order);
                            // Notify frontend that order is saved
                            await webView.CoreWebView2.PostWebMessageAsJsonAsync(JsonSerializer.Serialize(new { type = "buttonOrderSaved" }));
                            _logger.LogInformation("Button order saved successfully.");
                            await _terminalService.LogAsync("Button order saved successfully.", LogLevel.Information);
                        }
                        break;

                    case "openConfigDialog":
                        if (message.ContainsKey("button"))
                        {
                            var buttonJson = message["button"].ToString();
                            var button = JsonSerializer.Deserialize<ButtonConfig>(buttonJson);
                            OpenConfigDialog(button);
                        }
                        break;

                    case "addButton":
                        if (message.ContainsKey("button"))
                        {
                            var newButtonJson = message["button"].ToString();
                            var newButton = JsonSerializer.Deserialize<ButtonConfig>(newButtonJson);
                            await _toolbarService.AddButtonAsync(newButton);
                            _logger.LogInformation($"Button '{newButton.Label}' added via frontend.");
                            await _terminalService.LogAsync($"Button '{newButton.Label}' added via frontend.", LogLevel.Information);
                        }
                        break;

                    case "removeButton":
                        if (message.ContainsKey("buttonId"))
                        {
                            var buttonId = message["buttonId"].ToString();
                            await _toolbarService.RemoveButtonAsync(buttonId);
                            _logger.LogInformation($"Button with ID '{buttonId}' removed via frontend.");
                            await _terminalService.LogAsync($"Button with ID '{buttonId}' removed via frontend.", LogLevel.Information);
                        }
                        break;

                    // Handle other message types as needed

                    default:
                        _logger.LogWarning($"Unknown message type received: {messageType}");
                        await _terminalService.LogAsync($"Unknown message type received: {messageType}", LogLevel.Warning);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from frontend.");
                await _terminalService.LogAsync($"Error processing message from frontend: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Opens the configuration dialog for a specific button.
        /// </summary>
        /// <param name="button">The button configuration to edit.</param>
        private void OpenConfigDialog(ButtonConfig button)
        {
            try
            {
                var configWindow = new ButtonConfigWindow(button);
                configWindow.Owner = this;
                if (configWindow.ShowDialog() == true)
                {
                    // Update the button configuration via the toolbar service
                    _toolbarService.UpdateButtonAsync(button).Wait();

                    // Notify frontend to update the button
                    string updatedButtonJson = JsonSerializer.Serialize(button);
                    webView.CoreWebView2.ExecuteScriptAsync($"toolbar.updateButton({updatedButtonJson});").Wait();

                    _logger.LogInformation($"Button '{button.Label}' configuration updated via dialog.");
                    _terminalService.LogAsync($"Button '{button.Label}' configuration updated via dialog.", LogLevel.Information).Wait();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating button configuration for '{button.Label}'.");
                System.Windows.MessageBox.Show("Failed to update button configuration. Please check the logs for more details.", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _terminalService.LogAsync($"Error updating button configuration for '{button.Label}': {ex.Message}", LogLevel.Error).Wait();
            }
        }

        /// <summary>
        /// Initializes the system tray icon and its context menu.
        /// </summary>
        private void InitializeTrayIcon()
        {
            try
            {
                _trayIcon = new NotifyIcon
                {
                    Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
                    Visible = true,
                    Text = "Professional Toolbar"
                };

                var contextMenu = new ContextMenuStrip();
                contextMenu.Items.Add("Show/Hide Toolbar", null, ToggleVisibility);
                contextMenu.Items.Add("Open Terminal", null, OpenTerminal);
                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add("Exit", null, ExitApplication);

                _trayIcon.ContextMenuStrip = contextMenu;
                _trayIcon.DoubleClick += (s, e) => ToggleVisibility(s, e);

                _logger.LogInformation("System tray icon initialized.");
                _terminalService.LogAsync("System tray icon initialized.", LogLevel.Information).Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing system tray icon.");
                _terminalService.LogAsync($"Error initializing system tray icon: {ex.Message}", LogLevel.Error).Wait();
            }
        }

        /// <summary>
        /// Toggles the visibility of the main window.
        /// </summary>
        private void ToggleVisibility(object sender, EventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                this.Hide();
                _logger.LogInformation("Main window hidden via tray icon.");
                _terminalService.LogAsync("Main window hidden via tray icon.", LogLevel.Information).Wait();
            }
            else
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
                _logger.LogInformation("Main window shown via tray icon.");
                _terminalService.LogAsync("Main window shown via tray icon.", LogLevel.Information).Wait();
            }
        }

        /// <summary>
        /// Opens the terminal window.
        /// </summary>
        private void OpenTerminal(object sender, EventArgs e)
        {
            try
            {
                // Initialize and show the terminal window via TerminalService
                _terminalService.Initialize();
                _logger.LogInformation("Terminal window opened via tray icon.");
                _terminalService.LogAsync("Terminal window opened via tray icon.", LogLevel.Information).Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening terminal window via tray icon.");
                System.Windows.MessageBox.Show("Failed to open terminal window. Please check the logs for more details.", "Terminal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _terminalService.LogAsync($"Error opening terminal window: {ex.Message}", LogLevel.Error).Wait();
            }
        }

        /// <summary>
        /// Exits the application gracefully.
        /// </summary>
        private void ExitApplication(object sender, EventArgs e)
        {
            try
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                System.Windows.Application.Current.Shutdown();
                _logger.LogInformation("Application exited via tray icon.");
                _terminalService.LogAsync("Application exited via tray icon.", LogLevel.Information).Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exiting application via tray icon.");
                _terminalService.LogAsync($"Error exiting application: {ex.Message}", LogLevel.Error).Wait();
            }
        }

        /// <summary>
        /// Ensures that the tray icon is disposed when the main window is closed.
        /// </summary>
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
        }
    }

    /// <summary>
    /// Wrapper class exposing script execution functionalities to JavaScript.
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(true)]
    [System.Runtime.InteropServices.ClassInterface(System.Runtime.InteropServices.ClassInterfaceType.None)]
    public class ScriptExecutorJs
    {
        private readonly ToolbarService _toolbarService;
        private readonly ILogger _logger;

        public ScriptExecutorJs(ToolbarService toolbarService, ILogger logger)
        {
            _toolbarService = toolbarService;
            _logger = logger;
        }

        /// <summary>
        /// Executes a script asynchronously.
        /// </summary>
        /// <param name="scriptType">Type of the script: "PowerShell", "Cmd", "Python".</param>
        /// <param name="command">The script command or path.</param>
        /// <param name="adminRights">Whether to execute with admin rights.</param>
        /// <returns>The output of the script execution.</returns>
        public async Task<string> ExecuteScriptAsync(string scriptType, string command, bool adminRights)
        {
            try
            {
                // Implement script execution logic here
                // For example, invoke the ScriptExecutor service
                var scriptExecutor = _toolbarService.GetService<ScriptExecutor>();
                string result = await scriptExecutor.ExecuteScriptAsync(scriptType, command, adminRights);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing script from frontend.");
                return $"Error executing script: {ex.Message}";
            }
        }
    }

    /// <summary>
    /// Wrapper class exposing system operations to JavaScript.
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(true)]
    [System.Runtime.InteropServices.ClassInterface(System.Runtime.InteropServices.ClassInterfaceType.None)]
    public class SystemServiceJs
    {
        private readonly ToolbarService _toolbarService;
        private readonly ILogger _logger;

        public SystemServiceJs(ToolbarService toolbarService, ILogger logger)
        {
            _toolbarService = toolbarService;
            _logger = logger;
        }

        /// <summary>
        /// Executes an external application asynchronously.
        /// </summary>
        /// <param name="path">Path to the executable.</param>
        /// <param name="arguments">Arguments for the executable.</param>
        /// <param name="adminRights">Whether to run with admin rights.</param>
        /// <returns>A message indicating the execution status.</returns>
        public async Task<string> ExecuteApplicationAsync(string path, string arguments, bool adminRights)
        {
            try
            {
                // Implement application execution logic here
                var systemService = _toolbarService.GetService<SystemService>();
                await systemService.ExecuteApplicationAsync(path, arguments, adminRights);
                return $"Launched application: {Path.GetFileName(path)}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing application from frontend.");
                return $"Error executing application: {ex.Message}";
            }
        }

        /// <summary>
        /// Opens a URL in the default web browser.
        /// </summary>
        /// <param name="url">The URL to open.</param>
        /// <returns>A message indicating the result of the operation.</returns>
        public async Task<string> OpenUrlAsync(string url)
        {
            try
            {
                var systemService = _toolbarService.GetService<SystemService>();
                systemService.OpenUrl(url);
                return $"Opened URL: {url}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening URL from frontend.");
                return $"Error opening URL: {ex.Message}";
            }
        }
    }

    /// <summary>
    /// Wrapper class exposing plugin functionalities to JavaScript.
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(true)]
    [System.Runtime.InteropServices.ClassInterface(System.Runtime.InteropServices.ClassInterfaceType.None)]
    public class PluginServiceJs
    {
        private readonly ToolbarService _toolbarService;
        private readonly ILogger _logger;

        public PluginServiceJs(ToolbarService toolbarService, ILogger logger)
        {
            _toolbarService = toolbarService;
            _logger = logger;
        }

        /// <summary>
        /// Executes a plugin asynchronously.
        /// </summary>
        /// <param name="pluginId">The ID of the plugin to execute.</param>
        /// <returns>The result of the plugin execution.</returns>
        public async Task<string> ExecutePluginAsync(string pluginId)
        {
            try
            {
                // Implement plugin execution logic here
                var pluginService = _toolbarService.GetService<PluginService>();
                string result = await pluginService.ExecutePluginAsync(pluginId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing plugin from frontend.");
                return $"Error executing plugin: {ex.Message}";
            }
        }
    }
}
