using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ToolbarApp.Models;
using Microsoft.Extensions.Logging;

namespace ToolBarApp.Services
{
    /// <summary>
    /// Service responsible for loading and saving toolbar configurations.
    /// </summary>
    public class ConfigurationService
    {
        private readonly string _configFilePath;
        private required string _pluginDirectory;
        private required string _testConfigFilePath;

        private readonly ILogger<ConfigurationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationService"/> class with a specified configuration file path.
        /// </summary>
        /// <param name="configFilePath">The file path to the configuration file.</param>
        /// <param name="logger">The logger instance for logging.</param>
        public ConfigurationService(string configFilePath, ILogger<ConfigurationService> logger)
        {
            if (string.IsNullOrWhiteSpace(configFilePath))
            {
                throw new ArgumentException("Configuration file path cannot be null or whitespace.", nameof(configFilePath));
            }

            _configFilePath = configFilePath;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationService"/> class with the default configuration file path.
        /// </summary>
        /// <param name="logger">The logger instance for logging.</param>
        public ConfigurationService(ILogger<ConfigurationService> logger)
            : this(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "toolbar_config.json"), logger)
        {
        }

        /// <summary>
        /// Loads the toolbar configuration asynchronously.
        /// </summary>
        /// <returns>A <see cref="ToolbarConfig"/> object containing all toolbar configurations.</returns>
        public async Task<ToolbarConfig> LoadConfigurationAsync()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    string json = await File.ReadAllTextAsync(_configFilePath);
                    var config = JsonSerializer.Deserialize<ToolbarConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    });

                    if (config != null)
                    {
                        _logger.LogInformation("Configuration loaded successfully from {ConfigFilePath}.", _configFilePath);
                        return config;
                    }
                    else
                    {
                        _logger.LogWarning("Configuration file {ConfigFilePath} is empty or invalid. Loading default configuration.", _configFilePath);
                    }
                }
                else
                {
                    _logger.LogWarning("Configuration file {ConfigFilePath} does not exist. Loading default configuration.", _configFilePath);
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON deserialization error while loading configuration from {ConfigFilePath}. Loading default configuration.", _configFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while loading configuration from {ConfigFilePath}. Loading default configuration.", _configFilePath);
            }

            // Return default configuration if file doesn't exist or deserialization fails
            return new ToolbarConfig();
        }

        /// <summary>
        /// Saves the toolbar configuration asynchronously.
        /// </summary>
        /// <param name="config">The <see cref="ToolbarConfig"/> object to save.</param>
        public async Task SaveConfigurationAsync(ToolbarConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Configuration to save cannot be null.");
            }

            try
            {
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Ensure the directory exists
                string directory = Path.GetDirectoryName(_configFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(_configFilePath, json);
                _logger.LogInformation("Configuration saved successfully to {ConfigFilePath}.", _configFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving configuration to {ConfigFilePath}.", _configFilePath);
            }
        }
    }
}
