using System;
using System.Windows;
using ToolbarApp.Services;
using ToolbarApp.Models;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;

namespace ToolbarApp.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly ConfigurationService _configService;
        private readonly LoggingService _loggingService;
        private readonly List<ToolbarWindow> _toolbarWindows;

        public SettingsWindow(ConfigurationService configService, LoggingService loggingService, List<ToolbarWindow> toolbarWindows)
        {
            InitializeComponent();
            _configService = configService;
            _loggingService = loggingService;
            _toolbarWindows = toolbarWindows;
            LoadSettings();
            LoadThemes();
            LoadToolbars();
        }

        private async void LoadSettings()
        {
            var settings = await _configService.LoadConfigurationAsync();
            chkAlwaysOnTop.IsChecked = settings.IsAlwaysOnTop;
            chkAlerts.IsChecked = settings.AreAlertsEnabled;
        }

        private void LoadThemes()
        {
            // Assuming themes are stored under app/themes/themename/styles.css
            string themesPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app", "themes");
            if (System.IO.Directory.Exists(themesPath))
            {
                var themes = System.IO.Directory.GetDirectories(themesPath)
                                .Select(dir => System.IO.Path.GetFileName(dir))
                                .ToList();
                cmbThemes.ItemsSource = themes;
                cmbThemes.SelectedItem = "default"; // Default theme
            }
        }

        private async void LoadToolbars()
        {
            var settings = await _configService.LoadConfigurationAsync();
            lstToolbars.ItemsSource = settings.Toolbars;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var settings = await _configService.LoadConfigurationAsync();
            settings.IsAlwaysOnTop = chkAlwaysOnTop.IsChecked ?? false;
            settings.AreAlertsEnabled = chkAlerts.IsChecked ?? true;

            // Apply global theme if needed
            if (cmbThemes.SelectedItem != null)
            {
                settings.SelectedTheme = cmbThemes.SelectedItem.ToString();
                // Apply theme to all toolbars
                foreach (var toolbar in _toolbarWindows)
                {
                    toolbar.ChangeTheme(settings.SelectedTheme);
                }
            }

            await _configService.SaveConfigurationAsync(settings);
            _loggingService.Log("Application settings saved successfully.");
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private async void BtnAddToolbar_Click(object sender, RoutedEventArgs e)
        {
            var settings = await _configService.LoadConfigurationAsync();
            if (settings.Toolbars.Count >= 3)
            {
                System.Windows.MessageBox.Show("Maximum of 3 toolbars allowed.", "Limit Reached", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newToolbar = new ToolbarConfig
            {
                ToolbarId = $"toolbar{settings.Toolbars.Count + 1}",
                Position = "Top",
                IsPinned = false,
                Theme = "default",
                Buttons = new List<ButtonConfig>()
            };

            // Add sample buttons
            newToolbar.Buttons.AddRange(GetSampleButtons());

            settings.Toolbars.Add(newToolbar);
            await _configService.SaveConfigurationAsync(settings);
            LoadToolbars();

            // Create new toolbar window
            var newToolbarWindow = new ToolbarWindow(newToolbar, _configService, /* Inject other services as needed */ null, null, _loggingService, null);
            newToolbarWindow.Owner = this.Owner;
            newToolbarWindow.Show();
            _toolbarWindows.Add(newToolbarWindow);

            _loggingService.Log($"Added new toolbar '{newToolbar.ToolbarId}'.");
        }

        private async void BtnRemoveToolbar_Click(object sender, RoutedEventArgs e)
        {
            if (lstToolbars.SelectedItem is ToolbarConfig selectedToolbar)
            {
                var result = System.Windows.MessageBox.Show($"Are you sure you want to remove toolbar '{selectedToolbar.ToolbarId}'?", "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    var settings = await _configService.LoadConfigurationAsync();
                    settings.Toolbars.Remove(selectedToolbar);
                    await _configService.SaveConfigurationAsync(settings);
                    LoadToolbars();

                    // Close and remove the toolbar window
                    var toolbarWindow = _toolbarWindows.FirstOrDefault(t => t.Config.ToolbarId == selectedToolbar.ToolbarId);
                    if (toolbarWindow != null)
                    {
                        toolbarWindow.Close();
                        _toolbarWindows.Remove(toolbarWindow);
                    }

                    _loggingService.Log($"Removed toolbar '{selectedToolbar.ToolbarId}'.");
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Please select a toolbar to remove.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private List<ButtonConfig> GetSampleButtons()
        {
            return new List<ButtonConfig>
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
        }
    }
}
