using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ToolbarApp.Models;
using Microsoft.Extensions.Logging;

namespace ToolBarApp.Services
{
    /// <summary>
    /// Service responsible for loading and saving user and application settings.
    /// </summary>
    public class SettingsService
    {
        private readonly string _settingsFilePath;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(ILogger<SettingsService> logger)
        {
            _logger = logger;
            _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        }

        /// <summary>
        /// Loads the application settings asynchronously.
        /// </summary>
        /// <returns>A Settings object containing all user and application settings.</returns>
        public async Task<Settings> LoadSettingsAsync()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = await File.ReadAllTextAsync(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<Settings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings.");
            }
            // Return default settings if file doesn't exist or deserialization fails
            return new Settings();
        }

        /// <summary>
        /// Saves the application settings asynchronously.
        /// </summary>
        /// <param name="settings">The Settings object to save.</param>
        public async Task SaveSettingsAsync(Settings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_settingsFilePath, json);
                _logger.LogInformation("Settings saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings.");
            }
        }
    }
}
