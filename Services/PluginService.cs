// PluginService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ToolBarApp.Models;

namespace ToolBarApp.Services
{
    /// <summary>
    /// Service responsible for managing plugins.
    /// </summary>
    public class PluginService
    {        private readonly ILogger<PluginService> _logger;
        private readonly List<IToolbarPlugin> _loadedPlugins;
        private required string _pluginDirectory;
        private required string _testConfigFilePath;


        public PluginService(ILogger<PluginService> logger)
        {
            _logger = logger;
            _pluginsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            _loadedPlugins = new List<IToolbarPlugin>();

            if (!Directory.Exists(_pluginsDirectory))
            {
                Directory.CreateDirectory(_pluginsDirectory);
                _logger.LogInformation($"Created plugins directory at {_pluginsDirectory}.");
            }
        }

        /// <summary>
        /// Loads all plugins from the plugins directory.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task LoadPluginsAsync()
        {
            var pluginFiles = Directory.GetFiles(_pluginsDirectory, "*.dll");

            foreach (var file in pluginFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    var pluginTypes = assembly.GetTypes().Where(t => typeof(IToolbarPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var type in pluginTypes)
                    {
                        var plugin = (IToolbarPlugin)Activator.CreateInstance(type);
                        _loadedPlugins.Add(plugin);
                        _logger.LogInformation($"Loaded plugin: {plugin.Name}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error loading plugin from file: {file}");
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Executes a plugin by its ID.
        /// </summary>
        /// <param name="pluginId">The ID of the plugin to execute.</param>
        /// <returns>The result of the plugin execution.</returns>
        public async Task<string> ExecutePluginAsync(string pluginId)
        {
            var plugin = _loadedPlugins.FirstOrDefault(p => p.Id == pluginId);
            if (plugin != null)
            {
                try
                {
                    string result = await plugin.ExecuteAsync();
                    _logger.LogInformation($"Executed plugin: {plugin.Name}");
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error executing plugin: {plugin.Name}");
                    return $"Error executing plugin: {ex.Message}";
                }
            }
            else
            {
                _logger.LogWarning($"Plugin with ID '{pluginId}' not found.");
                return $"Plugin with ID '{pluginId}' not found.";
            }
        }

        /// <summary>
        /// Retrieves all loaded plugins.
        /// </summary>
        /// <returns>A list of loaded plugins.</returns>
        public List<IToolbarPlugin> GetLoadedPlugins()
        {
            return _loadedPlugins;
        }

        /// <summary>
        /// Loads a plugin from a specified path.
        /// </summary>
        /// <param name="pluginPath">The file path of the plugin DLL.</param>
        /// <returns>An instance of the loaded plugin.</returns>
        public IToolbarPlugin LoadPlugin(string pluginPath)
        {
            try
            {
                var assembly = Assembly.LoadFrom(pluginPath);
                var pluginType = assembly.GetTypes().FirstOrDefault(t => typeof(IToolbarPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
                if (pluginType != null)
                {
                    var plugin = (IToolbarPlugin)Activator.CreateInstance(pluginType);
                    _loadedPlugins.Add(plugin);
                    _logger.LogInformation($"Loaded plugin: {plugin.Name}");
                    return plugin;
                }
                else
                {
                    _logger.LogWarning($"No valid plugin found in assembly: {pluginPath}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading plugin from path: {pluginPath}");
                return null;
            }
        }

        /// <summary>
        /// Adds a plugin to the loaded plugins list.
        /// </summary>
        /// <param name="plugin">The plugin to add.</param>
        public void AddPlugin(IToolbarPlugin plugin)
        {
            if (plugin == null) throw new ArgumentNullException(nameof(plugin));
            _loadedPlugins.Add(plugin);
            _logger.LogInformation($"Added plugin: {plugin.Name}");
        }

        /// <summary>
        /// Removes a plugin by its name.
        /// </summary>
        /// <param name="pluginName">The name of the plugin to remove.</param>
        public void RemovePlugin(string pluginName)
        {
            var plugin = _loadedPlugins.FirstOrDefault(p => p.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
            if (plugin != null)
            {
                _loadedPlugins.Remove(plugin);
                _logger.LogInformation($"Removed plugin: {plugin.Name}");
            }
            else
            {
                _logger.LogWarning($"Plugin '{pluginName}' not found.");
            }
        }

        /// <summary>
        /// Saves the current plugin list. (Implementation depends on how plugins are managed)
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SavePluginsAsync()
        {
            // Implement plugin persistence logic if necessary
            await Task.CompletedTask;
        }
    }
}
