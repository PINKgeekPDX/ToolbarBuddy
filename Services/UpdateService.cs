using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Squirrel;
using System.Windows;

namespace ToolBarApp.Services
{
    /// <summary>
    /// Service responsible for managing application updates.
    /// Utilizes Squirrel.Windows for update operations.
    /// </summary>
    public class UpdateService
    {
        private readonly ILogger<UpdateService> _logger;
        private readonly string _updateUrl = "https://your-update-server.com/updates/"; // Replace with your actual update server URL

        public UpdateService(ILogger<UpdateService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Initializes the update service and checks for updates.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InitializeAsync()
        {
            try
            {
                using var mgr = await UpdateManager.GitHubUpdateManager(_updateUrl);
                await CheckForUpdatesAsync(mgr);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize update manager.");
            }
        }

        /// <summary>
        /// Checks for available updates and prompts the user to apply them.
        /// </summary>
        /// <param name="mgr">The UpdateManager instance.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task CheckForUpdatesAsync(UpdateManager mgr)
        {
            try
            {
                var updateInfo = await mgr.CheckForUpdate();
                if (updateInfo.ReleasesToApply.Count > 0)
                {
                    var result = MessageBox.Show($"Update {updateInfo.FutureReleaseEntry.Version} is available. Do you want to update now?",
                                                 "Update Available",
                                                 MessageBoxButton.YesNo,
                                                 MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        await mgr.DownloadReleases(updateInfo.ReleasesToApply);
                        await mgr.UpdateApp();
                        UpdateManager.RestartApp();
                    }
                }
                else
                {
                    _logger.LogInformation("No updates available.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check for updates.");
                MessageBox.Show("Failed to check for updates.", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Applies updates and restarts the application.
        /// </summary>
        /// <param name="mgr">The UpdateManager instance.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ApplyUpdatesAsync(UpdateManager mgr)
        {
            try
            {
                await mgr.UpdateApp();
                UpdateManager.RestartApp();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply updates.");
            }
        }
    }
}
