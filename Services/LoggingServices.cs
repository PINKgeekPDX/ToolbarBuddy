using Microsoft.Extensions.Logging;

namespace ToolBarApp.Services
{
    /// <summary>
    /// Service responsible for logging application events and errors.
    /// </summary>
    public class LoggingService
    {
        private readonly ILogger<LoggingService> _logger;

        public LoggingService(ILogger<LoggingService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogInfo(string message)
        {
            _logger.LogInformation(message);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogWarning(string message)
        {
            _logger.LogWarning(message);
        }

        /// <summary>
        /// Logs an error message with an exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="ex">The exception associated with the error.</param>
        public void LogError(string message, Exception ex)
        {
            _logger.LogError(ex, message);
        }

        /// <summary>
        /// General log method with specified log level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The severity level of the log.</param>
        public void Log(string message, LogLevel level = LogLevel.Information)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    _logger.LogTrace(message);
                    break;
                case LogLevel.Debug:
                    _logger.LogDebug(message);
                    break;
                case LogLevel.Information:
                    _logger.LogInformation(message);
                    break;
                case LogLevel.Warning:
                    _logger.LogWarning(message);
                    break;
                case LogLevel.Error:
                    _logger.LogError(message);
                    break;
                case LogLevel.Critical:
                    _logger.LogCritical(message);
                    break;
                default:
                    _logger.LogInformation(message);
                    break;
            }
        }
    }
}
