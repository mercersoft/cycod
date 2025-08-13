using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cycodlib.Abstractions
{
    /// <summary>
    /// Provides shell command execution abstraction for platform-agnostic command execution
    /// </summary>
    public interface IShellExecutor
    {
        /// <summary>
        /// Executes a shell command asynchronously
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <param name="shell">The shell type to use</param>
        /// <param name="timeoutMs">The timeout in milliseconds (0 for no timeout)</param>
        /// <returns>The result of the shell command execution</returns>
        Task<ShellResult> ExecuteAsync(string command, ShellType shell = ShellType.Default, int timeoutMs = 0);

        /// <summary>
        /// Gets a value indicating whether shell execution is available in the current environment
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Gets the available shell types in the current environment
        /// </summary>
        IEnumerable<ShellType> AvailableShells { get; }
    }

    /// <summary>
    /// Represents the result of a shell command execution
    /// </summary>
    public class ShellResult
    {
        /// <summary>
        /// Gets or sets the standard output from the command
        /// </summary>
        public string StandardOutput { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the standard error from the command
        /// </summary>
        public string StandardError { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the exit code of the command
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// Gets a value indicating whether the command succeeded (exit code 0)
        /// </summary>
        public bool Success => ExitCode == 0;

        /// <summary>
        /// Gets or sets a value indicating whether the command timed out
        /// </summary>
        public bool TimedOut { get; set; }

        /// <summary>
        /// Gets or sets the exception if the command failed to execute
        /// </summary>
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// Represents the type of shell to use for command execution
    /// </summary>
    public enum ShellType
    {
        /// <summary>
        /// Use the default shell for the platform
        /// </summary>
        Default,

        /// <summary>
        /// Use Bash shell
        /// </summary>
        Bash,

        /// <summary>
        /// Use PowerShell
        /// </summary>
        PowerShell,

        /// <summary>
        /// Use Windows Command Prompt
        /// </summary>
        Cmd,

        /// <summary>
        /// Use Zsh shell
        /// </summary>
        Zsh,

        /// <summary>
        /// Use Fish shell
        /// </summary>
        Fish
    }
}