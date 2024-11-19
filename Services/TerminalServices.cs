// Services/TerminalService.cs
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ToolBarApp.Views;

namespace ToolBarApp.Services
{
    /// <summary>
    /// Service responsible for managing terminal logs and communication.
    /// </summary>
    public class TerminalService
    {
        private readonly ILogger<TerminalService> _logger;
        private TerminalWindow _terminalWindow;

        public TerminalService(ILogger<TerminalService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes and shows the TerminalWindow.
        /// </summary>
        public void Initialize()
        {
            if (_terminalWindow == null)
            {
                _terminalWindow = new TerminalWindow();
                _terminalWindow.Show();
                _logger.LogInformation("Terminal window initialized and displayed.");
            }
            else
            {
                _terminalWindow.Activate();
                _logger.LogInformation("Terminal window activated.");
            }
        }

        /// <summary>
        /// Sends a log message to the TerminalWindow.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The log level.</param>
        public async Task LogAsync(string message, LogLevel level = LogLevel.Information)
        {
            if (_terminalWindow == null)
            {
                _logger.LogWarning("TerminalWindow is not initialized.");
                return;
            }

            string logMessage = $"{DateTime.Now:HH:mm:ss} [{level}] {message}";
            await _terminalWindow.AppendMessageAsync(logMessage, level);
        }
    }
}
