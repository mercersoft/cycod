using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Cycodlib.Abstractions;
using Cycod.Implementation;

namespace MenuBarWebSocketApp.Services;

public class CommandResult
{
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public bool TimedOut { get; set; }
    public Exception? Exception { get; set; }
}

public class CommandExecutionService
{
    private readonly IServiceProvider _serviceProvider;
    private static CycoDevProgramRunner? _sharedProgramRunner = null;
    private static readonly object _lock = new object();

    public CommandExecutionService()
    {
        // Create service provider for cycod dependencies
        var services = new ServiceCollection();
        
        // Register the same services as in cycod Program.cs
        services.AddSingleton<Cycodlib.Abstractions.ILogger>(provider => 
        {
            var config = provider.GetService<Cycodlib.Abstractions.IConfigurationProvider>();
            var isDebugEnabled = config?.GetConfigValue("debug") == "true";
            var isQuietMode = config?.GetConfigValue("quiet") == "true";
            return ConsoleLogger.ForComponent("CommandExecution", !isQuietMode);
        });
        
        services.AddSingleton<Cycodlib.Abstractions.IConfigurationProvider, ConsoleConfigurationProvider>();
        services.AddSingleton<IStorageProvider>(provider =>
        {
            var logger = provider.GetService<Cycodlib.Abstractions.ILogger>();
            return new FileStorageProvider(maxRetries: 3, logger: logger);
        });
        services.AddSingleton<IShellExecutor>(provider =>
        {
            var logger = provider.GetService<Cycodlib.Abstractions.ILogger>();
            return new ConsoleShellExecutor(logger);
        });

        _serviceProvider = services.BuildServiceProvider();
        
        // Initialize shared program runner if not already done
        lock (_lock)
        {
            if (_sharedProgramRunner == null)
            {
                try
                {
                    _sharedProgramRunner = new CycoDevProgramRunner(_serviceProvider);
                    Console.WriteLine("[COMMAND EXEC] Shared CycoDevProgramRunner initialized successfully");
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("ProgramInfo is already initialized"))
                {
                    Console.WriteLine("[COMMAND EXEC] ProgramInfo already initialized, this is expected");
                    // For now, we'll work around this by using the static method
                    _sharedProgramRunner = null;
                }
            }
        }
    }

    public async Task<CommandResult> ExecuteCommandAsync(string command, string[] args, int timeoutMs = 30000)
    {
        try
        {
            // Validate that this is a cycod command
            if (command != "cycod")
            {
                return new CommandResult
                {
                    StandardError = $"Only 'cycod' commands are supported. Received: {command}",
                    ExitCode = 1
                };
            }

            // Validate config commands
            if (args.Length < 1 || args[0] != "config")
            {
                return new CommandResult
                {
                    StandardError = "Only 'cycod config' commands are supported currently",
                    ExitCode = 1
                };
            }

            // Validate config subcommands
            if (args.Length < 2)
            {
                return new CommandResult
                {
                    StandardError = "Config command requires a subcommand (list, get, set, clear, add, remove)",
                    ExitCode = 1
                };
            }

            var configSubcommand = args[1].ToLower();
            var validSubcommands = new[] { "list", "get", "set", "clear", "add", "remove" };
            if (Array.IndexOf(validSubcommands, configSubcommand) == -1)
            {
                return new CommandResult
                {
                    StandardError = $"Invalid config subcommand: {configSubcommand}. Valid subcommands: {string.Join(", ", validSubcommands)}",
                    ExitCode = 1
                };
            }

            // Execute the command using CycoDevProgramRunner
            return await ExecuteCycodCommandInternalAsync(args, timeoutMs);
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                StandardError = $"Command execution failed: {ex.Message}",
                ExitCode = -1,
                Exception = ex
            };
        }
    }

    private async Task<CommandResult> ExecuteCycodCommandInternalAsync(string[] args, int timeoutMs)
    {
        Console.WriteLine($"[COMMAND EXEC] Starting execution of: cycod {string.Join(" ", args)}");
        
        // Capture stdout and stderr
        var stdoutCapture = new StringWriter();
        var stderrCapture = new StringWriter();
        
        var originalOut = Console.Out;
        var originalError = Console.Error;
        
        int exitCode = 0;
        bool timedOut = false;
        Exception? exception = null;

        try
        {
            Console.WriteLine($"[COMMAND EXEC] Redirecting console output");
            
            // Redirect console output
            Console.SetOut(stdoutCapture);
            Console.SetError(stderrCapture);

            // Create a cancellation token for timeout
            using var cts = new CancellationTokenSource(timeoutMs);
            
            Console.WriteLine($"[COMMAND EXEC] About to call CycoDevProgramRunner.RunAsync");
            
            try
            {
                // For now, always use direct processing to avoid ProgramInfo issues
                exitCode = await ProcessConfigCommandDirectly(args, stdoutCapture);
                Console.WriteLine($"[COMMAND EXEC] Direct config processing completed with exit code: {exitCode}");
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                Console.WriteLine($"[COMMAND EXEC] Command timed out");
                timedOut = true;
                exitCode = -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[COMMAND EXEC] Exception occurred: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"[COMMAND EXEC] Stack trace: {ex.StackTrace}");
                exception = ex;
                exitCode = -1;
            }
        }
        finally
        {
            // Restore console output
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Console.WriteLine($"[COMMAND EXEC] Console output restored");
        }

        var stdoutContent = stdoutCapture.ToString();
        var stderrContent = stderrCapture.ToString();
        
        // Filter out server logging messages from command output
        var filteredStdout = FilterServerMessages(stdoutContent);
        var filteredStderr = FilterServerMessages(stderrContent);
        
        Console.WriteLine($"[COMMAND EXEC] Results - ExitCode: {exitCode}, TimedOut: {timedOut}");
        Console.WriteLine($"[COMMAND EXEC] Raw stdout length: {stdoutContent.Length}, Filtered: {filteredStdout.Length}");
        Console.WriteLine($"[COMMAND EXEC] Raw stderr length: {stderrContent.Length}, Filtered: {filteredStderr.Length}");
        if (!string.IsNullOrEmpty(filteredStdout))
        {
            Console.WriteLine($"[COMMAND EXEC] Filtered stdout: {filteredStdout}");
        }
        if (!string.IsNullOrEmpty(filteredStderr))
        {
            Console.WriteLine($"[COMMAND EXEC] Filtered stderr: {filteredStderr}");
        }

        var result = new CommandResult
        {
            StandardOutput = filteredStdout,
            StandardError = filteredStderr,
            ExitCode = exitCode,
            TimedOut = timedOut,
            Exception = exception
        };

        // Add timeout message to stderr if timed out
        if (timedOut)
        {
            result.StandardError += $"\nCommand timed out after {timeoutMs}ms";
        }

        // Add exception message to stderr if there was an exception (but not for ProgramInfo initialization)
        if (exception != null && !exception.Message.Contains("ProgramInfo is already initialized"))
        {
            result.StandardError += $"\nException: {exception.Message}";
        }

        Console.WriteLine($"[COMMAND EXEC] Final result - ExitCode: {result.ExitCode}, Stdout: '{result.StandardOutput}', Stderr: '{result.StandardError}'");
        return result;
    }

    private string FilterServerMessages(string output)
    {
        if (string.IsNullOrEmpty(output))
            return output;

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var filteredLines = lines.Where(line => 
            !line.Contains("[COMMAND EXEC]") &&
            !line.Contains("[ICON UPDATE]") &&
            !line.Contains("[TOOLTIP]") &&
            !line.Trim().StartsWith("🐛") &&
            !string.IsNullOrWhiteSpace(line)
        ).ToArray();

        return string.Join('\n', filteredLines);
    }

    private Task<int> ProcessConfigCommandDirectly(string[] args, StringWriter output)
    {
        try
        {
            // Real implementation using ConfigStore like cycod does
            if (args.Length < 2 || args[0] != "config")
            {
                output.WriteLine("Only config commands are supported in direct mode");
                return Task.FromResult(1);
            }

            var subcommand = args[1].ToLower();
            
            switch (subcommand)
            {
                case "list":
                    return ProcessConfigListCommand(args, output);

                case "get":
                    return ProcessConfigGetCommand(args, output);

                default:
                    output.WriteLine($"Config subcommand '{subcommand}' not supported in direct mode");
                    output.WriteLine("Supported commands: list, get");
                    return Task.FromResult(1);
            }
        }
        catch (Exception ex)
        {
            output.WriteLine($"Error in direct config processing: {ex.Message}");
            return Task.FromResult(1);
        }
    }

    private Task<int> ProcessConfigListCommand(string[] args, StringWriter output)
    {
        try
        {
            var configStore = ConfigStore.Instance;

            // Display config settings in the same order as cycod config list
            
            // Global scope
            var globalLocation = ConfigFileHelpers.GetLocationDisplayName(ConfigFileScope.Global) ?? "Global";
            var globalValues = configStore.ListValuesFromKnownScope(ConfigFileScope.Global);
            DisplayConfigSettings(output, globalLocation, globalValues);
            output.WriteLine();

            // User scope  
            var userLocation = ConfigFileHelpers.GetLocationDisplayName(ConfigFileScope.User) ?? "User";
            var userValues = configStore.ListValuesFromKnownScope(ConfigFileScope.User);
            DisplayConfigSettings(output, userLocation, userValues);
            output.WriteLine();

            // Local scope
            var localLocation = ConfigFileHelpers.GetLocationDisplayName(ConfigFileScope.Local) ?? "Local";
            var localValues = configStore.ListValuesFromKnownScope(ConfigFileScope.Local);
            DisplayConfigSettings(output, localLocation, localValues);
            output.WriteLine();

            // FileName scope (custom config files)
            var fileNameToConfigValues = configStore.ListFileNameScopeValues();
            foreach (var kvp in fileNameToConfigValues)
            {
                var location = $"{kvp.Key} (specified)";
                DisplayConfigSettings(output, location, kvp.Value);
                output.WriteLine();
            }

            // Command line settings
            var commandLineValues = configStore.ListFromCommandLineSettings();
            if (commandLineValues.Count > 0)
            {
                var location = "Command line (specified)";
                DisplayConfigSettings(output, location, commandLineValues);
                output.WriteLine();
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            output.WriteLine($"Error listing configuration: {ex.Message}");
            return Task.FromResult(1);
        }
    }

    private Task<int> ProcessConfigGetCommand(string[] args, StringWriter output)
    {
        try
        {
            if (args.Length < 3)
            {
                output.WriteLine("Usage: config get <key>");
                return Task.FromResult(1);
            }

            var configStore = ConfigStore.Instance;
            var key = args[2];
            var configValue = configStore.GetFromAnyScope(key);

            if (!configValue.IsNotFoundNullOrEmpty())
            {
                var displayValue = configValue.IsSecret
                    ? configValue.AsObfuscated() ?? "(empty)"
                    : configValue.Value?.ToString() ?? "(null)";
                output.WriteLine($"{key}: {displayValue}");
                return Task.FromResult(0);
            }
            else
            {
                output.WriteLine($"Configuration key '{key}' not found");
                return Task.FromResult(1);
            }
        }
        catch (Exception ex)
        {
            output.WriteLine($"Error getting configuration value: {ex.Message}");
            return Task.FromResult(1);
        }
    }

    private void DisplayConfigSettings(StringWriter output, string location, Dictionary<string, ConfigValue> config, int indentLevel = 2)
    {
        // Write location header
        output.WriteLine($"{location}:");

        if (config.Count == 0)
        {
            output.WriteLine($"{new string(' ', indentLevel)}No configuration settings found.");
            return;
        }

        // Sort the keys with non-dotted keys first, then dotted keys (same as ConfigDisplayHelpers)
        var sortedKeys = SortKeysWithNonDottedFirst(config.Keys);
        
        bool hasDisplayedNonDotted = false;
        bool hasDisplayedDotted = false;

        foreach (var key in sortedKeys)
        {
            // If we're switching from non-dotted to dotted keys, add a line break
            if (!hasDisplayedDotted && key.Contains('.'))
            {
                if (hasDisplayedNonDotted)
                {
                    output.WriteLine();
                }
                hasDisplayedDotted = true;
            }
            
            if (!key.Contains('.'))
            {
                hasDisplayedNonDotted = true;
            }
            
            DisplayConfigValue(output, key, config[key], indentLevel);
        }
    }

    private void DisplayConfigValue(StringWriter output, string key, ConfigValue value, int indentLevel = 2)
    {
        var indent = new string(' ', indentLevel);
        
        // If it's a list type in memory (actual List objects)
        if (value.Value is List<object> || value.Value is List<string>)
        {
            var list = value.AsList();
            DisplayList(output, key, list, indentLevel);
            return;
        }
        
        // Get value to display, obfuscating if it's a secret
        var displayValue = value.IsSecret
            ? value.AsObfuscated() ?? "(empty)"
            : !value.IsNotFoundNullOrEmpty()
                ? value.Value?.ToString() ?? "(null)"
                : "(not found or empty)";
                            
        output.WriteLine($"{indent}{key}: {displayValue}");
    }

    private void DisplayList(StringWriter output, string key, List<string> list, int indentLevel = 2)
    {
        var keyIndent = new string(' ', indentLevel);
        var valueIndent = new string(' ', indentLevel + 2);
        
        if (list.Count > 0)
        {
            output.WriteLine($"{keyIndent}{key}:");
            foreach (var item in list)
            {
                output.WriteLine($"{valueIndent}- {item}");
            }
        }
        else
        {
            output.WriteLine($"{keyIndent}{key}: (empty list)");
        }
    }

    private List<string> SortKeysWithNonDottedFirst(IEnumerable<string> keys)
    {
        // Split keys into two groups: non-dotted and dotted
        var nonDottedKeys = new List<string>();
        var dottedKeys = new List<string>();
        
        foreach (var key in keys)
        {
            if (key.Contains('.'))
            {
                dottedKeys.Add(key);
            }
            else
            {
                nonDottedKeys.Add(key);
            }
        }
        
        // Sort each group alphabetically
        nonDottedKeys.Sort(StringComparer.OrdinalIgnoreCase);
        dottedKeys.Sort(StringComparer.OrdinalIgnoreCase);
        
        // Combine the groups with non-dotted keys first
        var sortedKeys = new List<string>();
        sortedKeys.AddRange(nonDottedKeys);
        sortedKeys.AddRange(dottedKeys);
        
        return sortedKeys;
    }

    public void Dispose()
    {
        if (_serviceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }
    }
}