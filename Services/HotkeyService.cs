using System;
using System.Windows.Input;
using NHotkey;
using NHotkey.Wpf;
using Microsoft.Extensions.Logging;
using ToolbarApp.Views;

namespace ToolBarApp.Services
{
    /// <summary>
    /// Service responsible for registering and handling global hotkeys.
    /// </summary>
    public class HotkeyService
    {
        private readonly ILogger<HotkeyService> _logger;
        private readonly MainWindow _mainWindow;

        public HotkeyService(MainWindow mainWindow, ILogger<HotkeyService> logger)
        {
            _mainWindow = mainWindow;
            _logger = logger;
            RegisterHotkeys();
        }

        /// <summary>
        /// Registers all necessary global hotkeys.
        /// </summary>
        private void RegisterHotkeys()
        {
            try
            {
                // Ctrl + F1 to toggle toolbar visibility
                HotkeyManager.Current.AddOrReplace("ToggleToolbar", Key.F1, ModifierKeys.Control, OnToggleToolbar);

                // Ctrl + Shift + T to open Terminal
                HotkeyManager.Current.AddOrReplace("OpenTerminal", Key.T, ModifierKeys.Control | ModifierKeys.Shift, OnOpenTerminal);

                // Ctrl + Shift + S to open Settings
                HotkeyManager.Current.AddOrReplace("OpenSettings", Key.S, ModifierKeys.Control | ModifierKeys.Shift, OnOpenSettings);

                _logger.LogInformation("Global hotkeys registered successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register global hotkeys.");
            }
        }

        /// <summary>
        /// Handler for the ToggleToolbar hotkey.
        /// Toggles the visibility of the main toolbar.
        /// </summary>
        private void OnToggleToolbar(object sender, HotkeyEventArgs e)
        {
            _mainWindow.ToggleToolbarVisibility();
            _logger.LogInformation("ToggleToolbar hotkey pressed.");
            e.Handled = true;
        }

        /// <summary>
        /// Handler for the OpenTerminal hotkey.
        /// Opens or toggles the terminal window.
        /// </summary>
        private void OnOpenTerminal(object sender, HotkeyEventArgs e)
        {
            _mainWindow.OpenTerminal();
            _logger.LogInformation("OpenTerminal hotkey pressed.");
            e.Handled = true;
        }

        /// <summary>
        /// Handler for the OpenSettings hotkey.
        /// Opens the settings window.
        /// </summary>
        private void OnOpenSettings(object sender, HotkeyEventArgs e)
        {
            _mainWindow.OpenSettingsDialog();
            _logger.LogInformation("OpenSettings hotkey pressed.");
            e.Handled = true;
        }

        /// <summary>
        /// Unregisters all hotkeys. Should be called on application exit.
        /// </summary>
        public void UnregisterHotkeys()
        {
            HotkeyManager.Current.Remove("ToggleToolbar");
            HotkeyManager.Current.Remove("OpenTerminal");
            HotkeyManager.Current.Remove("OpenSettings");
            _logger.LogInformation("Global hotkeys unregistered.");
        }
    }
}
