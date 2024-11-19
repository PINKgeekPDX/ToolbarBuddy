// ToolbarService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToolBarApp.Models;
using Microsoft.Extensions.Logging;

namespace ToolBarApp.Services
{
    /// <summary>
    /// Service responsible for managing toolbar operations.
    /// </summary>
    public class ToolbarService
    {
        private readonly ConfigurationService _configService;
        private readonly ILogger<ToolbarService> _logger;

        public ToolbarService(ConfigurationService configService, ILogger<ToolbarService> logger)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Adds a new button to the toolbar and updates the configuration.
        /// </summary>
        /// <param name="button">The button configuration to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddButtonAsync(ButtonConfig button)
        {
            if (button == null) throw new ArgumentNullException(nameof(button));

            var config = await _configService.LoadConfigurationAsync();
            if (config.Toolbars == null || !config.Toolbars.Any())
            {
                // Initialize with a default toolbar if none exist
                config.Toolbars = new List<SingleToolbarConfig>
                {
                    new SingleToolbarConfig
                    {
                        Position = "Top",
                        IsPinned = true,
                        IsAlwaysOnTop = false,
                        Buttons = new List<ButtonConfig>()
                    }
                };
            }

            var toolbar = config.Toolbars.First(); // Assuming single toolbar for simplicity
            toolbar.Buttons.Add(button);
            await _configService.SaveConfigurationAsync(config);

            _logger.LogInformation($"Button '{button.Label}' added to the toolbar.");
        }

        /// <summary>
        /// Updates an existing button's configuration.
        /// </summary>
        /// <param name="updatedButton">The updated button configuration.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpdateButtonAsync(ButtonConfig updatedButton)
        {
            if (updatedButton == null) throw new ArgumentNullException(nameof(updatedButton));

            var config = await _configService.LoadConfigurationAsync();
            var toolbar = config.Toolbars.FirstOrDefault();
            if (toolbar == null)
            {
                _logger.LogWarning("No toolbar found to update the button.");
                return;
            }

            var button = toolbar.Buttons.FirstOrDefault(b => b.Id == updatedButton.Id);
            if (button != null)
            {
                // Update properties
                button.Label = updatedButton.Label;
                button.Type = updatedButton.Type;
                button.Config = updatedButton.Config;
                button.Tooltip = updatedButton.Tooltip;

                await _configService.SaveConfigurationAsync(config);
                _logger.LogInformation($"Button '{button.Label}' updated in the toolbar.");
            }
            else
            {
                _logger.LogWarning($"Button with ID '{updatedButton.Id}' not found in the toolbar.");
            }
        }

        /// <summary>
        /// Removes a button from the toolbar based on its ID.
        /// </summary>
        /// <param name="buttonId">The ID of the button to remove.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task RemoveButtonAsync(string buttonId)
        {
            if (string.IsNullOrWhiteSpace(buttonId)) throw new ArgumentException("Button ID cannot be null or whitespace.", nameof(buttonId));

            var config = await _configService.LoadConfigurationAsync();
            var toolbar = config.Toolbars.FirstOrDefault();
            if (toolbar == null)
            {
                _logger.LogWarning("No toolbar found to remove the button.");
                return;
            }

            var button = toolbar.Buttons.FirstOrDefault(b => b.Id == buttonId);
            if (button != null)
            {
                toolbar.Buttons.Remove(button);
                await _configService.SaveConfigurationAsync(config);
                _logger.LogInformation($"Button '{button.Label}' removed from the toolbar.");
            }
            else
            {
                _logger.LogWarning($"Button with ID '{buttonId}' not found in the toolbar.");
            }
        }

        /// <summary>
        /// Reorders buttons in the toolbar based on the provided order.
        /// </summary>
        /// <param name="orderedIds">List of button IDs in the desired order.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ReorderButtonsAsync(List<string> orderedIds)
        {
            if (orderedIds == null) throw new ArgumentNullException(nameof(orderedIds));

            var config = await _configService.LoadConfigurationAsync();
            var toolbar = config.Toolbars.FirstOrDefault();
            if (toolbar == null)
            {
                _logger.LogWarning("No toolbar found to reorder buttons.");
                return;
            }

            var orderedButtons = orderedIds.Select(id => toolbar.Buttons.FirstOrDefault(b => b.Id == id)).Where(b => b != null).ToList();

            if (orderedButtons.Count != toolbar.Buttons.Count)
            {
                _logger.LogWarning("Mismatch between provided order and existing buttons. Reordering aborted.");
                return;
            }

            toolbar.Buttons = orderedButtons;
            await _configService.SaveConfigurationAsync(config);
            _logger.LogInformation("Toolbar buttons reordered successfully.");
        }

        /// <summary>
        /// Loads the toolbar configuration.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, containing the toolbar configuration.</returns>
        public async Task<ToolbarConfig> LoadToolbarConfigAsync()
        {
            return await _configService.LoadConfigurationAsync();
        }
    }
}
