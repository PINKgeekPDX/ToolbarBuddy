using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ToolBarApp.Services;
using ToolBarApp.Services.HostObjects;

namespace ToolbarApp.Services.HostObjects
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Obsolete]
    public class ScriptExecutorJs(ScriptExecutor scriptExecutor, ILogger<ScriptExecutorJs> logger)
    {
        private readonly ScriptExecutor _scriptExecutor = scriptExecutor ?? throw new ArgumentNullException(nameof(scriptExecutor));
        private readonly ILogger<ScriptExecutorJs> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task<string> ExecuteScriptAsync(string scriptType, string command, bool adminRights)
        {
            try
            {
                string result = await _scriptExecutor.ExecuteScriptAsync(scriptType, command, adminRights);
                _logger.LogInformation("Executed {ScriptType} script: {Command}", scriptType, command);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute script: {Command}", command);
                return $"Error executing script: {ex.Message}";
            }
        }
    }
}
