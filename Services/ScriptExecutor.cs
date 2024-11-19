using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ToolBarApp.Services
{
    public class ScriptExecutor
    {
        private readonly ILogger<ScriptExecutor> _logger;
        private const int ExecutionTimeoutSeconds = 30;

        public ScriptExecutor(ILogger<ScriptExecutor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes a script asynchronously and returns the output.
        /// </summary>
        /// <param name="scriptType">Type of the script: "PowerShell", "Cmd", "Python".</param>
        /// <param name="command">The script command or path.</param>
        /// <param name="adminRights">Whether to execute with admin rights.</param>
        /// <returns>The output of the script execution.</returns>
        public async Task<string> ExecuteScriptAsync(string scriptType, string command, bool adminRights)
        {
            try
            {
                _logger.LogInformation("Executing {ScriptType} script: {Command}", scriptType, command);

                var startInfo = new ProcessStartInfo
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                ConfigureProcessStartInfo(startInfo, scriptType, command);

                if (adminRights)
                {
                    startInfo.Verb = "runas";
                    startInfo.UseShellExecute = true;
                    startInfo.RedirectStandardOutput = false;
                    startInfo.RedirectStandardError = false;
                }

                using var process = new Process { StartInfo = startInfo };
                var output = new StringBuilder();
                var error = new StringBuilder();

                if (!adminRights)
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            output.AppendLine(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            error.AppendLine(e.Data);
                        }
                    };
                }

                process.Start();

                if (!adminRights)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }

                bool processExited = await WaitForProcessAsync(process);

                if (!processExited)
                {
                    try
                    {
                        process.Kill();
                        return "Process execution timed out and was terminated.";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error terminating process");
                        return "Process execution timed out but could not be terminated.";
                    }
                }

                string result = adminRights ? "Command executed with admin rights" : output.ToString();
                if (error.Length > 0)
                {
                    result = string.Concat(result, Environment.NewLine, "Errors:", Environment.NewLine, error.ToString());
                }

                return result.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing script: {ScriptType} - {Command}", scriptType, command);
                throw;
            }
        }

        private void ConfigureProcessStartInfo(ProcessStartInfo startInfo, string scriptType, string command)
        {
            switch (scriptType.ToLower())
            {
                case "powershell":
                    startInfo.FileName = "powershell.exe";
                    startInfo.Arguments = string.Format("-NoProfile -ExecutionPolicy Bypass -Command \"{0}\"", command);
                    break;

                case "cmd":
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = string.Format("/c {0}", command);
                    break;

                case "python":
                    startInfo.FileName = "python.exe";
                    startInfo.Arguments = string.Format("-c \"{0}\"", command);
                    break;

                default:
                    throw new ArgumentException(string.Format("Unsupported script type: {0}", scriptType));
            }
        }

        private async Task<bool> WaitForProcessAsync(Process process)
        {
            var processCompletionSource = new TaskCompletionSource<bool>();

            process.Exited += (sender, args) =>
            {
                processCompletionSource.TrySetResult(true);
            };

            process.EnableRaisingEvents = true;

            if (process.HasExited)
            {
                return true;
            }

            using var cancellationToken = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(ExecutionTimeoutSeconds));
            cancellationToken.Token.Register(() => processCompletionSource.TrySetResult(false));

            return await processCompletionSource.Task;
        }
    }
}
