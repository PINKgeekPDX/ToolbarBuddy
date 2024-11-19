/*
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System.Windows.Forms;
using ToolBarApp.Models;
using ToolBarApp.Services;
using ToolBarApp.Services.Interfaces;

namespace ToolBarApp.Tests
{
    public class ConfigurationServiceTests : IDisposable
    {
        private readonly string _testConfigFilePath;
        private readonly Mock<ILogger<ConfigurationService>> _mockLogger;
        private readonly ConfigurationService _configurationService;

        public ConfigurationServiceTests()
        {
            // Setup a temporary configuration file
            _testConfigFilePath = Path.Combine(Path.GetTempPath(), "ToolBarApp_TestConfig.json");
            if (File.Exists(_testConfigFilePath))
            {
                File.Delete(_testConfigFilePath);
            }

            // Setup Mock Logger
            _mockLogger = new Mock<ILogger<ConfigurationService>>();

            // Initialize ConfigurationService with the test config file path
            _configurationService = new ConfigurationServiceForTest(_testConfigFilePath, _mockLogger.Object);
        }

        [Fact]
        public async Task LoadConfigurationAsync_ShouldReturnDefaultWhenFileDoesNotExist()
        {
            // Arrange
            // Ensure the config file does not exist
            if (File.Exists(_testConfigFilePath))
            {
                File.Delete(_testConfigFilePath);
            }

            // Act
            var config = await _configurationService.LoadConfigurationAsync();

            // Assert
            Assert.NotNull(config);
            Assert.NotNull(config.ToolbarConfig);
            Assert.Empty(config.ToolbarConfig.Toolbars);
            _mockLogger.Verify(
                x => x.Log(
                    It.Is<Serilog.Events.LogEvent>(le => le.Level == Serilog.Events.LogEventLevel.Information && le.MessageTemplate.Text.Contains("return default configuration")),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task LoadConfigurationAsync_ShouldLoadExistingConfiguration()
        {
            // Arrange
            var expectedConfig = new AppConfig
            {
                ToolbarConfig = new ToolbarConfig
                {
                    Toolbars =
                    [
                        new() 
                        {
                            Position = "Top",
                            IsPinned = true,
                            IsAlwaysOnTop = false,
                            Buttons =
                            [
                                new() 
                                {
                                    Label = "Test Button",
                                    Type = "script",
                                    Config = new System.Collections.Generic.Dictionary<string, object>
                                    {
                                        { "scriptType", "PowerShell" },
                                        { "command", "Get-Process" },
                                        { "adminRights", false }
                                    }
                                }
                            ]
                        }
                    ]
                }
            };

            var json = JsonSerializer.Serialize(expectedConfig, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_testConfigFilePath, json);

            // Act
            var config = await _configurationService.LoadConfigurationAsync();

            // Assert
            Assert.NotNull(config);
            Assert.NotNull(config.ToolbarConfig);
            Assert.Single(config.ToolbarConfig.Toolbars);
            Assert.Equal("Top", config.ToolbarConfig.Toolbars[0].Position);
            Assert.True(config.ToolbarConfig.Toolbars[0].IsPinned);
            Assert.False(config.ToolbarConfig.Toolbars[0].IsAlwaysOnTop);
            Assert.Single(config.ToolbarConfig.Toolbars[0].Buttons);
            Assert.Equal("Test Button", config.ToolbarConfig.Toolbars[0].Buttons[0].Label);
            Assert.Equal("script", config.ToolbarConfig.Toolbars[0].Buttons[0].Type);
            Assert.Equal("PowerShell", config.ToolbarConfig.Toolbars[0].Buttons[0].Config["scriptType"]);
            Assert.Equal("Get-Process", config.ToolbarConfig.Toolbars[0].Buttons[0].Config["command"]);
            Assert.False((bool)config.ToolbarConfig.Toolbars[0].Buttons[0].Config["adminRights"]);

            _mockLogger.Verify(
                x => x.Log(
                    It.Is<Serilog.Events.LogEvent>(le => le.Level == Serilog.Events.LogEventLevel.Information && le.MessageTemplate.Text.Contains("loaded configuration successfully")),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task SaveConfigurationAsync_ShouldWriteConfigurationToFile()
        {
            // Arrange
            var configToSave = new AppConfig
            {
                ToolbarConfig = new ToolbarConfig
                {
                    Toolbars =
                    [
                        new() 
                        {
                            Position = "Bottom",
                            IsPinned = false,
                            IsAlwaysOnTop = true,
                            Buttons =
                            [
                                new() 
                                {
                                    Label = "Save Button",
                                    Type = "application",
                                    Config = new System.Collections.Generic.Dictionary<string, object>
                                    {
                                        { "path", "notepad.exe" },
                                        { "arguments", "" },
                                        { "adminRights", false }
                                    }
                                }
                            ]
                        }
                    ]
                }
            };

            // Act
            await _configurationService.SaveConfigurationAsync(configToSave);

            // Assert
            Assert.True(File.Exists(_testConfigFilePath));
            var savedJson = await File.ReadAllTextAsync(_testConfigFilePath);
            var savedConfig = JsonSerializer.Deserialize<AppConfig>(savedJson);

            Assert.NotNull(savedConfig);
            Assert.NotNull(savedConfig.ToolbarConfig);
            Assert.Single(savedConfig.ToolbarConfig.Toolbars);
            Assert.Equal("Bottom", savedConfig.ToolbarConfig.Toolbars[0].Position);
            Assert.False(savedConfig.ToolbarConfig.Toolbars[0].IsPinned);
            Assert.True(savedConfig.ToolbarConfig.Toolbars[0].IsAlwaysOnTop);
            Assert.Single(savedConfig.ToolbarConfig.Toolbars[0].Buttons);
            Assert.Equal("Save Button", savedConfig.ToolbarConfig.Toolbars[0].Buttons[0].Label);
            Assert.Equal("application", savedConfig.ToolbarConfig.Toolbars[0].Buttons[0].Type);
            Assert.Equal("notepad.exe", savedConfig.ToolbarConfig.Toolbars[0].Buttons[0].Config["path"]);
            Assert.Equal("", savedConfig.ToolbarConfig.Toolbars[0].Buttons[0].Config["arguments"]);
            Assert.False((bool)savedConfig.ToolbarConfig.Toolbars[0].Buttons[0].Config["adminRights"]);

            _mockLogger.Verify(
                x => x.Log(
                    It.Is<Serilog.Events.LogEvent>(le => le.Level == Serilog.Events.LogEventLevel.Information && le.MessageTemplate.Text.Contains("Configuration saved successfully")),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task LoadConfigurationAsync_ShouldHandleDeserializationErrorsGracefully()
        {
            // Arrange
            // Write invalid JSON to the config file
            await File.WriteAllTextAsync(_testConfigFilePath, "Invalid JSON Content");

            // Act
            var config = await _configurationService.LoadConfigurationAsync();

            // Assert
            Assert.NotNull(config);
            Assert.NotNull(config.ToolbarConfig);
            Assert.Empty(config.ToolbarConfig.Toolbars);

            _mockLogger.Verify(
                x => x.Log(
                    It.Is<Serilog.Events.LogEvent>(le => le.Level == Serilog.Events.LogEventLevel.Error && le.MessageTemplate.Text.Contains("Error loading configuration.")),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        // Dispose method to clean up the temporary config file after tests
        public void Dispose()
        {
            if (File.Exists(_testConfigFilePath))
            {
                try
                {
                    File.Delete(_testConfigFilePath);
                }
                catch
                {
                    // If deletion fails, ignore to prevent exceptions during test teardown
                }
            }
        }
    }

    // Custom ConfigurationService for testing to use the test config file path
    public class ConfigurationServiceForTest : ConfigurationService
    {
        private readonly string _testConfigFilePath;

        public ConfigurationServiceForTest(string testConfigFilePath, ILogger<ConfigurationService> logger)
            : base(logger)
        {
            _testConfigFilePath = testConfigFilePath;
        }

        protected override string GetConfigFilePath()
        {
            return _testConfigFilePath;
        }
    }
}
*/