using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ToolbarApp.Services;
using ToolbarApp.Models;
using System.Windows;
using ToolBarApp.Services;

namespace ToolbarApp.Services.HostObjects
{
    /// <summary>
    /// Wrapper class to expose SystemService methods to JavaScript via WebView2.
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Obsolete]
    public class SystemServiceJs(SystemService systemService, LoggingService loggingService)
    {

        /// <summary>
        /// Executes an external application asynchronously.
        /// </summary>
        /// <param name="path">Path to the executable.</param>
        /// <param name="arguments">Arguments for the executable.</param>
        /// <param name="adminRights">Whether to run with admin rights.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the execution status.</returns>
        public async Task<string> ExecuteApplicationAsync(string path, string arguments, bool adminRights)
        {
            try
            {
                await systemService.ExecuteApplicationAsync(path, arguments, adminRights);
                loggingService.LogInfo($"Executed application: {path} with arguments: {arguments}");
                return $"Application '{path}' executed successfully.";
            }
            catch (Exception ex)
            {
                loggingService.LogError($"Failed to execute application: {path}", ex);
                return $"Error executing application: {ex.Message}";
            }
        }

        /// <summary>
        /// Opens a URL in the default web browser.
        /// </summary>
        /// <param name="url">The URL to open.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the status message.</returns>
        public Task<string> OpenUrlAsync(string url)
        {
            try
            {
                systemService.OpenUrl(url);
                loggingService.LogInfo($"Opened URL: {url}");
                return Task.FromResult($"URL '{url}' opened successfully.");
            }
            catch (Exception ex)
            {
                loggingService.LogError($"Failed to open URL: {url}", ex);
                return Task.FromResult($"Error opening URL: {ex.Message}");
            }
        }
    }
}
