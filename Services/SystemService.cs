// SystemService.cs
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ToolBarApp.Services
{
    /// <summary>
    /// Service responsible for system-level operations like launching applications and opening URLs.
    /// </summary>
    public class SystemService
    {
        private readonly ILogger<SystemService> _logger;

        public SystemService(ILogger<SystemService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Executes an external application asynchronously.
        /// </summary>
        /// <param name="path">Path to the executable.</param>
        /// <param name="arguments">Arguments for the executable.</param>
        /// <param name="adminRights">Whether to run with admin rights.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ExecuteApplicationAsync(string path, string arguments, bool adminRights)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = arguments,
                    UseShellExecute = true
                };

                if (adminRights)
                {
                    processStartInfo.Verb = "runas";
                }

                using var process = Process.Start(processStartInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    _logger.LogInformation($"Executed application: {path} with arguments: {arguments}");
                }
                else
                {
                    throw new InvalidOperationException($"Failed to start application: {path}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing application: {path}");
                throw;
            }
        }

        /// <summary>
        /// Opens a URL in the default web browser.
        /// </summary>
        /// <param name="url">The URL to open.</param>
        public void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                _logger.LogInformation($"Opened URL: {url}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error opening URL: {url}");
                throw;
            }
        }

        /// <summary>
        /// Opens a URL asynchronously in the default web browser.
        /// </summary>
        /// <param name="url">The URL to open.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task OpenUrlAsync(string url)
        {
            await Task.Run(() => OpenUrl(url));
        }
    }
}
