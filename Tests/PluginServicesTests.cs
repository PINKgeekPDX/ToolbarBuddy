/*
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System.Windows.Forms;
using ToolBarApp.Models;
using ToolBarApp.Services;
using ToolBarApp.Services.Interfaces;

namespace ToolBarApp.Tests
{
    public class PluginServicesTests : IDisposable
    {
        private readonly string _testPluginDirectory;
        private readonly Mock<ILogger<PluginService>> _mockLogger;
        private readonly ConfigurationService _configurationService;
        private readonly PluginService _pluginService;

        public PluginServicesTests()
        {
            // Setup a temporary directory for plugins
            _testPluginDirectory = Path.Combine(Path.GetTempPath(), "ToolBarApp_TestPlugins");
            if (!Directory.Exists(_testPluginDirectory))
            {
                Directory.CreateDirectory(_testPluginDirectory);
            }

            // Initialize ConfigurationService with test plugin directory
            _configurationService = new ConfigurationServiceForTest(_testPluginDirectory);

            // Setup Mock Logger
            _mockLogger = new Mock<ILogger<PluginService>>();

            // Initialize PluginService with mocked dependencies
            _pluginService = new PluginService(_mockLogger.Object, _configurationService);
        }

        [Fact]
        public async Task LoadPluginsAsync_ShouldLoadValidPlugins()
        {
            // Arrange
            // Create a mock plugin assembly
            string pluginPath = CreateMockPlugin("TestPlugin1");
            await Task.CompletedTask;

            // Act
            _pluginService.LoadAllPlugins();

            // Assert
            var loadedPlugins = _pluginService.GetLoadedPlugins();
            Assert.Single(loadedPlugins);
            Assert.Equal("TestPlugin1", loadedPlugins.First().Name);
        }

        [Fact]
        public void LoadPluginsAsync_ShouldIgnoreInvalidPlugins()
        {
            // Arrange
            // Create an invalid plugin assembly (does not implement IToolbarPlugin)
            string invalidPluginPath = Path.Combine(_testPluginDirectory, "InvalidPlugin.dll");
            File.WriteAllBytes(invalidPluginPath, [0x00, 0x01, 0x02]); // Invalid DLL

            // Act
            _pluginService.LoadAllPlugins();

            // Assert
            var loadedPlugins = _pluginService.GetLoadedPlugins();
            Assert.Empty(loadedPlugins);

            // Verify that an error was logged
            _mockLogger.Verify(
                x => x.Log(
                    It.Is<Serilog.Events.LogEvent>(le => le.Level == Serilog.Events.LogEventLevel.Error),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void AddPluginAsync_ShouldAddValidPlugin()
        {
            // Arrange
            string pluginPath = CreateMockPlugin("TestPlugin2");

            // Act
            var plugin = _pluginService.LoadPlugin(pluginPath);
            _pluginService.AddPlugin(plugin);

            // Assert
            var loadedPlugins = _pluginService.GetLoadedPlugins();
            Assert.Single(loadedPlugins);
            Assert.Equal("TestPlugin2", loadedPlugins.First().Name);
        }

        [Fact]
        public void AddPluginAsync_ShouldNotAddDuplicatePlugin()
        {
            // Arrange
            string pluginPath = CreateMockPlugin("TestPlugin3");

            // Act
            var plugin1 = _pluginService.LoadPlugin(pluginPath);
            _pluginService.AddPlugin(plugin1);
            var plugin2 = _pluginService.LoadPlugin(pluginPath);
            _pluginService.AddPlugin(plugin2); // Attempt to add duplicate

            // Assert
            var loadedPlugins = _pluginService.GetLoadedPlugins();
            Assert.Single(loadedPlugins); // Should still have only one instance
        }

        [Fact]
        public void RemovePluginAsync_ShouldRemoveExistingPlugin()
        {
            // Arrange
            string pluginPath = CreateMockPlugin("TestPlugin4");
            var plugin = _pluginService.LoadPlugin(pluginPath);
            _pluginService.AddPlugin(plugin);
            Assert.Single(_pluginService.GetLoadedPlugins());

            // Act
            _pluginService.RemovePlugin("TestPlugin4");

            // Assert
            var loadedPlugins = _pluginService.GetLoadedPlugins();
            Assert.Empty(loadedPlugins);
        }

        [Fact]
        public async Task ExecutePluginAsync_ShouldExecuteExistingPlugin()
        {
            // Arrange
            string pluginPath = CreateMockPlugin("TestPlugin5");
            var plugin = _pluginService.LoadPlugin(pluginPath);
            _pluginService.AddPlugin(plugin);

            // Act
            var executionResult = await _pluginService.ExecutePluginAsync("TestPlugin5");

            // Assert
            Assert.Equal("Plugin 'TestPlugin5' executed successfully.", executionResult);

            // Verify that an information log was created
            _mockLogger.Verify(
                x => x.Log(
                    It.Is<Serilog.Events.LogEvent>(le => le.Level == Serilog.Events.LogEventLevel.Information && le.MessageTemplate.Text.Contains("Executed plugin")),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecutePluginAsync_ShouldReturnErrorForNonExistentPlugin()
        {
            // Act
            var executionResult = await _pluginService.ExecutePluginAsync("NonExistentPlugin");

            // Assert
            Assert.Equal("Plugin with ID 'NonExistentPlugin' not found.", executionResult);

            // Verify that a warning log was created
            _mockLogger.Verify(
                x => x.Log(
                    It.Is<Serilog.Events.LogEvent>(le => le.Level == Serilog.Events.LogEventLevel.Warning && le.MessageTemplate.Text.Contains("not found")),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        // Helper method to create a mock plugin assembly that implements IToolbarPlugin
        private string CreateMockPlugin(string pluginName)
        {
            string assemblyName = $"{pluginName}.dll";
            string assemblyPath = Path.Combine(_testPluginDirectory, assemblyName);

            var assemblyNameDef = new AssemblyName(pluginName);
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyNameDef, AssemblyBuilderAccess.Save, _testPluginDirectory);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(pluginName, assemblyNameDef.Name + ".dll");

            // Define a public class that implements IToolbarPlugin
            var typeBuilder = moduleBuilder.DefineType("ToolBarApp.Plugins." + pluginName,
                TypeAttributes.Public | TypeAttributes.Class,
                null,
                [typeof(IToolbarPlugin)]);

            // Implement the Name property
            var nameProperty = typeBuilder.DefineProperty("Name", PropertyAttributes.HasDefault, typeof(string), null);
            var getNameMethod = typeBuilder.DefineMethod("get_Name",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                typeof(string),
                Type.EmptyTypes);

            var ilGen = getNameMethod.GetILGenerator();
            ilGen.Emit(OpCodes.Ldstr, pluginName);
            ilGen.Emit(OpCodes.Ret);

            nameProperty.SetGetMethod(getNameMethod);

            // Implement the GetButtonConfig method
            var getButtonConfigMethod = typeBuilder.DefineMethod("GetButtonConfig",
                MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(ButtonConfig),
                Type.EmptyTypes);

            ilGen = getButtonConfigMethod.GetILGenerator();
            ilGen.Emit(OpCodes.Newobj, typeof(ButtonConfig).GetConstructor(Type.EmptyTypes));
            ilGen.Emit(OpCodes.Dup);
            ilGen.Emit(OpCodes.Ldstr, pluginName + " Button");
            ilGen.Emit(OpCodes.Callvirt, typeof(ButtonConfig).GetProperty("Label").GetSetMethod());
            ilGen.Emit(OpCodes.Dup);
            ilGen.Emit(OpCodes.Ldstr, "plugin");
            ilGen.Emit(OpCodes.Callvirt, typeof(ButtonConfig).GetProperty("Type").GetSetMethod());
            // For simplicity, leave Config empty
            ilGen.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(getNameMethod, typeof(IToolbarPlugin).GetProperty("Name").GetGetMethod());
            typeBuilder.DefineMethodOverride(getButtonConfigMethod, typeof(IToolbarPlugin).GetMethod("GetButtonConfig"));

            // Implement the OnButtonClick method
            var onButtonClickMethod = typeBuilder.DefineMethod("OnButtonClick",
                MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(void),
                Type.EmptyTypes);

            ilGen = onButtonClickMethod.GetILGenerator();
            ilGen.Emit(OpCodes.Ret); // No operation

            typeBuilder.DefineMethodOverride(onButtonClickMethod, typeof(IToolbarPlugin).GetMethod("OnButtonClick"));

            // Create the type and save the assembly
            typeBuilder.CreateType();
            assemblyBuilder.Save(assemblyNameDef.Name + ".dll");

            return assemblyPath;
        }

        // Dispose method to clean up the temporary plugin directory after tests
        public void Dispose()
        {
            if (Directory.Exists(_testPluginDirectory))
            {
                try
                {
                    Directory.Delete(_testPluginDirectory, true);
                }
                catch
                {
                    // If deletion fails, ignore to prevent exceptions during test teardown
                }
            }
        }
    }

    // Custom ConfigurationService for testing to use the test plugin directory
    public class ConfigurationServiceForTest : ConfigurationService
    {
        private readonly string _pluginDirectory;

        public ConfigurationServiceForTest(string pluginDirectory)
        {
            _pluginDirectory = pluginDirectory;
        }

        protected override string GetPluginDirectory()
        {
            return _pluginDirectory;
        }
    }
}
*/