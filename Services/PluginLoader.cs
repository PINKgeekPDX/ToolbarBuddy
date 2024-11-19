using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using ToolBarApp.Services.Interfaces;

namespace ToolBarApp.Services
{
    public class PluginLoader
    {
        private readonly string _pluginDirectory;
        private readonly ILogger<PluginLoader> _logger;
        private readonly List<Assembly> _loadedPlugins;

        public PluginLoader(ILogger<PluginLoader> logger, string pluginDirectory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pluginDirectory = pluginDirectory ?? throw new ArgumentNullException(nameof(pluginDirectory));
            _loadedPlugins = new List<Assembly>();
        }

        public IEnumerable<Assembly> LoadedPlugins => _loadedPlugins.AsReadOnly();

        public void LoadPlugins()
        {
            try
            {
                if (!Directory.Exists(_pluginDirectory))
                {
                    _logger.LogInformation($"Creating plugin directory: {_pluginDirectory}");
                    Directory.CreateDirectory(_pluginDirectory);
                }

                var dllFiles = Directory.GetFiles(_pluginDirectory, "*.dll");
                if (!dllFiles.Any())
                {
                    _logger.LogInformation("No plugins found in the plugin directory.");
                    return;
                }

                foreach (var dll in dllFiles)
                {
                    LoadPlugin(dll);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugins from directory: {PluginDirectory}", _pluginDirectory);
                throw new PluginLoadException("Failed to load plugins.", ex);
            }
        }

        private void LoadPlugin(string dllPath)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllPath);
                _loadedPlugins.Add(assembly);
                _logger.LogInformation("Loaded plugin: {PluginName}", Path.GetFileName(dllPath));

                // Initialize plugins
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IToolbarPlugin).IsAssignableFrom(t) 
                           && !t.IsInterface 
                           && !t.IsAbstract);

                foreach (var type in pluginTypes)
                {
                    try
                    {
                        var pluginInstance = (IToolbarPlugin)Activator.CreateInstance(type);
                        pluginInstance.Initialize();
                        _logger.LogInformation("Initialized plugin: {PluginName}", pluginInstance.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to initialize plugin type: {PluginType}", type.FullName);
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                _logger.LogError(ex, "Failed to load plugin types from: {PluginPath}. Loader exceptions: {LoaderExceptions}", 
                    dllPath, 
                    string.Join(Environment.NewLine, ex.LoaderExceptions.Select(e => e.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin: {PluginPath}", dllPath);
            }
        }
    }

    public class PluginLoadException : Exception
    {
        public PluginLoadException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
