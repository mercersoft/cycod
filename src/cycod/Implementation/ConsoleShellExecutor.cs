using Cycodlib.Abstractions;

namespace Cycod.Implementation
{
    /// <summary>
    /// Console implementation of IShellExecutor using existing shell session infrastructure
    /// </summary>
    public class ConsoleShellExecutor : IShellExecutor
    {
        private readonly ILogger? _logger;

        public ConsoleShellExecutor(ILogger? logger = null)
        {
            _logger = logger;
        }

        public async Task<ShellResult> ExecuteAsync(string command, ShellType shell = ShellType.Default, int timeoutMs = 0)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return new ShellResult
                {
                    StandardError = "Command cannot be empty",
                    ExitCode = -1
                };
            }

            try
            {
                _logger?.WriteDebug($"Executing shell command: {command} (Shell: {shell}, Timeout: {timeoutMs}ms)");

                var shellSession = GetShellSession(shell);
                if (shellSession == null)
                {
                    return new ShellResult
                    {
                        StandardError = $"Shell type {shell} is not available on this platform",
                        ExitCode = -1
                    };
                }

                var result = await ExecuteWithTimeout(shellSession, command, timeoutMs);
                
                _logger?.WriteDebug($"Shell command completed with exit code: {result.ExitCode}");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger?.WriteError($"Shell command execution failed: {ex.Message}");
                return new ShellResult
                {
                    StandardError = ex.Message,
                    ExitCode = -1,
                    Exception = ex
                };
            }
        }

        public bool IsAvailable => true; // Console apps always have shell access

        public IEnumerable<ShellType> AvailableShells
        {
            get
            {
                var shells = new List<ShellType> { ShellType.Default };

                if (OperatingSystem.IsWindows())
                {
                    shells.AddRange(new[] { ShellType.Cmd, ShellType.PowerShell });
                }

                if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                {
                    shells.AddRange(new[] { ShellType.Bash, ShellType.Zsh, ShellType.Fish });
                }

                return shells;
            }
        }

        private static ShellSession? GetShellSession(ShellType shellType)
        {
            try
            {
                return shellType switch
                {
                    ShellType.Default => GetDefaultShell(),
                    ShellType.Bash => new BashShellSession(),
                    ShellType.PowerShell => new PowershellShellSession(),
                    ShellType.Cmd => new CmdShellSession(),
                    ShellType.Zsh => new BashShellSession(), // Zsh is compatible with bash session
                    ShellType.Fish => new BashShellSession(), // Fish fallback to bash session
                    _ => GetDefaultShell()
                };
            }
            catch
            {
                return null;
            }
        }

        private static ShellSession GetDefaultShell()
        {
            if (OperatingSystem.IsWindows())
            {
                return new PowershellShellSession();
            }
            else
            {
                return new BashShellSession();
            }
        }

        private async Task<ShellResult> ExecuteWithTimeout(ShellSession shellSession, string command, int timeoutMs)
        {
            var tcs = new TaskCompletionSource<ShellResult>();

            try
            {
                // Execute command in a separate task
                var executeTask = ExecuteCommandAsync(shellSession, command);

                Task completedTask;
                if (timeoutMs > 0)
                {
                    var timeoutTask = Task.Delay(timeoutMs);
                    completedTask = await Task.WhenAny(executeTask, timeoutTask);
                }
                else
                {
                    completedTask = executeTask;
                    await completedTask;
                }

                if (completedTask == executeTask)
                {
                    return await executeTask;
                }
                else
                {
                    // Timeout occurred
                    return new ShellResult
                    {
                        StandardError = $"Command timed out after {timeoutMs}ms",
                        ExitCode = -1,
                        TimedOut = true
                    };
                }
            }
            catch (Exception ex)
            {
                return new ShellResult
                {
                    StandardError = ex.Message,
                    ExitCode = -1,
                    Exception = ex
                };
            }
        }

        private static async Task<ShellResult> ExecuteCommandAsync(ShellSession shellSession, string command)
        {
            try
            {
                var output = await shellSession.ExecuteCommandAsync(command);
                
                return new ShellResult
                {
                    StandardOutput = output.stdout ?? string.Empty,
                    StandardError = output.stderr ?? string.Empty,
                    ExitCode = output.exitCode
                };
            }
            catch (Exception ex)
            {
                return new ShellResult
                {
                    StandardError = ex.Message,
                    ExitCode = -1,
                    Exception = ex
                };
            }
        }
    }
}