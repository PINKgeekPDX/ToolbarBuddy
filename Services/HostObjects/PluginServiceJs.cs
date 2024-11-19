using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ToolbarApp.Services;
using ToolbarApp.Models;
using ToolbarApp.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using ToolBarApp.Services;

namespace ToolbarApp.Services.HostObjects
{
    /// <summary>
    /// Wrapper class to expose PluginService methods to JavaScript via WebView2.
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class PluginServiceJs(PluginService pluginService, LoggingService logger)
    {

        /// <summary>
        /// Retrieves a list of all loaded plugins.
        /// </summary>
        /// <returns>A list of plugin names.</returns>
        public Task<string> GetPluginsAsync()
        {
            try
            {
                var plugins = pluginService.GetLoadedPlugins().Select(p => p.Name).ToList();
                var json = System.Text.Json.JsonSerializer.Serialize(plugins);
                return Task.FromResult(json);
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to retrieve plugins: {ex.Message}", ex);
                return Task.FromResult("Error retrieving plugins.");
            }
        }

        /// <summary>
        /// Executes a plugin's primary action asynchronously.
        /// </summary>
        /// <param name="pluginName">The name of the plugin to execute.</param>
        /// <returns>A message indicating the execution status.</returns>
        public async Task<string> ExecutePluginAsync(string pluginName)
        {
            try
            {
                var result = await pluginService.ExecutePluginAsync(pluginName);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError($"Plugin execution failed: {ex.Message}", ex);
                return $"Error executing plugin: {ex.Message}";
            }
        }

        /// <summary>
        /// Adds a new plugin dynamically.
        /// </summary>
        /// <param name="pluginPath">The file path to the plugin DLL.</param>
        /// <returns>A message indicating the result of the operation.</returns>
        public async Task<string> AddPluginAsync(string pluginPath)
        {
            try
            {
                var plugin = pluginService.LoadPlugin(pluginPath);
                if (plugin != null)
                {
                    pluginService.AddPlugin(plugin);
                    await pluginService.SavePluginsAsync();
                    logger.LogInformation($"Plugin added: {plugin.Name}");
                    return $"Plugin '{plugin.Name}' added successfully.";
                }
                else
                {
                    return "Failed to load plugin. Ensure it implements IToolbarPlugin.";
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to add plugin: {ex.Message}", ex);
                return $"Error adding plugin: {ex.Message}";
            }
        }

        /// <summary>
        /// Removes an existing plugin by name.
        /// </summary>
        /// <param name="pluginName">The name of the plugin to remove.</param>
        /// <returns>A message indicating the result of the operation.</returns>
        public async Task<string> RemovePluginAsync(string pluginName)
        {
            try
            {
                pluginService.RemovePlugin(pluginName);
                await pluginService.SavePluginsAsync();
                logger.LogInformation($"Plugin removed: {pluginName}");
                return $"Plugin '{pluginName}' removed successfully.";
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to remove plugin: {ex.Message}", ex);
                return $"Error removing plugin: {ex.Message}";
            }
        }
    }
}
