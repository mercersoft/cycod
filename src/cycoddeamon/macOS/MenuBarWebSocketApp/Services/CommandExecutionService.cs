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

                case "set":
                    return ProcessConfigSetCommand(args, output);

                case "clear":
                    return ProcessConfigClearCommand(args, output);

                case "add":
                    return ProcessConfigAddCommand(args, output);

                case "remove":
                    return ProcessConfigRemoveCommand(args, output);

                default:
                    output.WriteLine($"Config subcommand '{subcommand}' not supported in direct mode");
                    output.WriteLine("Supported commands:");
                    output.WriteLine("  list [--scope]  - List configuration settings");
                    output.WriteLine("  get <key> [--scope] - Get a configuration value");
                    output.WriteLine("  set <key> <value> [--scope] - Set a configuration value");
                    output.WriteLine("  clear <key> [--scope] - Clear a configuration setting");
                    output.WriteLine("  add <key> <value> [--scope] - Add value to list setting");
                    output.WriteLine("  remove <key> <value> [--scope] - Remove value from list setting");
                    output.WriteLine("Scope options: --global, --user, --local, --any");
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
            
            // Parse scope from remaining arguments
            var scope = ParseScopeFromArgs(args, 2, ConfigFileScope.Any); // Default to Any for list

            // Display config settings based on requested scope
            if (scope == ConfigFileScope.Any)
            {
                // Display all scopes in the same order as cycod config list
                
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
            }
            else
            {
                // Display only the requested scope
                var location = ConfigFileHelpers.GetLocationDisplayName(scope) ?? scope.ToString();
                var values = configStore.ListValuesFromKnownScope(scope);
                DisplayConfigSettings(output, location, values);
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
                output.WriteLine("Usage: config get <key> [--scope]");
                output.WriteLine("Scope options: --global, --user, --local, --any (default)");
                output.WriteLine("Example: config get debug --user");
                return Task.FromResult(1);
            }

            var configStore = ConfigStore.Instance;
            var key = args[2];
            
            // Parse scope from remaining arguments
            var scope = ParseScopeFromArgs(args, 3, ConfigFileScope.Any); // Default to Any for get
            
            ConfigValue configValue;
            if (scope == ConfigFileScope.Any)
            {
                configValue = configStore.GetFromAnyScope(key);
            }
            else
            {
                configValue = configStore.GetFromScope(key, scope);
            }

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
                var scopeText = scope == ConfigFileScope.Any ? "any scope" : $"{scope.ToString().ToLower()} scope";
                output.WriteLine($"Configuration key '{key}' not found in {scopeText}");
                return Task.FromResult(1);
            }
        }
        catch (Exception ex)
        {
            output.WriteLine($"Error getting configuration value: {ex.Message}");
            return Task.FromResult(1);
        }
    }

    private Task<int> ProcessConfigSetCommand(string[] args, StringWriter output)
    {
        try
        {
            if (args.Length < 4)
            {
                output.WriteLine("Usage: config set <key> <value> [--scope]");
                output.WriteLine("Scope options: --global, --user, --local (default)");
                output.WriteLine("Examples:");
                output.WriteLine("  config set debug true");
                output.WriteLine("  config set mylist [item1,item2,item3]");
                output.WriteLine("  config set api_key secret123 --user");
                return Task.FromResult(1);
            }

            var configStore = ConfigStore.Instance;
            var key = args[2];
            var value = args[3];
            
            // Parse scope from remaining arguments
            var scope = ParseScopeFromArgs(args, 4, ConfigFileScope.Local); // Default to Local for set

            // Try to parse as a list if the value is enclosed in brackets
            if (value.StartsWith("[") && value.EndsWith("]"))
            {
                var listContent = value.Substring(1, value.Length - 2);
                var listValue = new List<string>();
                
                if (!string.IsNullOrWhiteSpace(listContent))
                {
                    var items = listContent.Split(',');
                    foreach (var item in items)
                    {
                        listValue.Add(item.Trim());
                    }
                }
                
                // Set the list value
                var success = configStore.Set(key, listValue, scope, true);
                if (!success)
                {
                    output.WriteLine($"Failed to set configuration key '{key}' in {scope.ToString().ToLower()} scope");
                    return Task.FromResult(1);
                }

                // Display the set value
                DisplayList(output, key, listValue);
            }
            else
            {
                // Set the string value
                var success = configStore.Set(key, value, scope, true);
                if (!success)
                {
                    output.WriteLine($"Failed to set configuration key '{key}' in {scope.ToString().ToLower()} scope");
                    return Task.FromResult(1);
                }

                // Get and display the set value with location info
                var configValue = configStore.GetFromScope(key, scope);
                DisplayConfigValueWithLocation(output, key, configValue);
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            output.WriteLine($"Error setting configuration value: {ex.Message}");
            return Task.FromResult(1);
        }
    }

    private Task<int> ProcessConfigClearCommand(string[] args, StringWriter output)
    {
        try
        {
            if (args.Length < 3)
            {
                output.WriteLine("Usage: config clear <key> [--scope]");
                output.WriteLine("Scope options: --global, --user, --local (default)");
                output.WriteLine("Examples:");
                output.WriteLine("  config clear debug");
                output.WriteLine("  config clear api_key --user");
                return Task.FromResult(1);
            }

            var configStore = ConfigStore.Instance;
            var key = args[2];
            
            // Parse scope from remaining arguments
            var scope = ParseScopeFromArgs(args, 3, ConfigFileScope.Local); // Default to Local for clear

            // Clear the configuration value
            var success = configStore.Clear(key, scope, true);

            if (success)
            {
                output.WriteLine($"{key}: (cleared)");
            }
            else
            {
                output.WriteLine($"{key}: (not found)");
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            output.WriteLine($"Error clearing configuration value: {ex.Message}");
            return Task.FromResult(1);
        }
    }

    private Task<int> ProcessConfigAddCommand(string[] args, StringWriter output)
    {
        try
        {
            if (args.Length < 4)
            {
                output.WriteLine("Usage: config add <key> <value> [--scope]");
                output.WriteLine("Scope options: --global, --user, --local (default)");
                output.WriteLine("Examples:");
                output.WriteLine("  config add trusted_domains example.com");
                output.WriteLine("  config add custom_features experimental_mode --global");
                return Task.FromResult(1);
            }

            var configStore = ConfigStore.Instance;
            var key = args[2];
            var value = args[3];
            
            // Parse scope from remaining arguments
            var scope = ParseScopeFromArgs(args, 4, ConfigFileScope.Local); // Default to Local for add

            // Add the value to the list
            var success = configStore.AddToList(key, value, scope, true);
            if (!success)
            {
                output.WriteLine($"Failed to add value to configuration key '{key}' in {scope.ToString().ToLower()} scope");
                return Task.FromResult(1);
            }

            // Get and display the updated list
            var configValue = configStore.GetFromScope(key, scope);
            var listValue = configValue.AsList();
            
            if (listValue.Count > 0)
            {
                output.WriteLine($"{key}:");
                foreach (var item in listValue)
                {
                    output.WriteLine($"  - {item}");
                }
            }
            else
            {
                output.WriteLine($"{key}: (empty list)");
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            output.WriteLine($"Error adding to configuration list: {ex.Message}");
            return Task.FromResult(1);
        }
    }

    private Task<int> ProcessConfigRemoveCommand(string[] args, StringWriter output)
    {
        try
        {
            if (args.Length < 4)
            {
                output.WriteLine("Usage: config remove <key> <value> [--scope]");
                output.WriteLine("Scope options: --global, --user, --local (default)");
                output.WriteLine("Examples:");
                output.WriteLine("  config remove trusted_domains example.com");
                output.WriteLine("  config remove custom_features experimental_mode --global");
                return Task.FromResult(1);
            }

            var configStore = ConfigStore.Instance;
            var key = args[2];
            var value = args[3];
            
            // Parse scope from remaining arguments
            var scope = ParseScopeFromArgs(args, 4, ConfigFileScope.Local); // Default to Local for remove

            // Remove the value from the list
            var success = configStore.RemoveFromList(key, value, scope, true);
            if (!success)
            {
                output.WriteLine($"Failed to remove value from configuration key '{key}' in {scope.ToString().ToLower()} scope");
                return Task.FromResult(1);
            }

            // Get and display the updated list
            var configValue = configStore.GetFromScope(key, scope);
            var listValue = configValue.AsList();
            
            if (listValue.Count > 0)
            {
                output.WriteLine($"{key}:");
                foreach (var item in listValue)
                {
                    output.WriteLine($"  - {item}");
                }
            }
            else
            {
                output.WriteLine($"{key}: (empty list)");
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            output.WriteLine($"Error removing from configuration list: {ex.Message}");
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

    private ConfigFileScope ParseScopeFromArgs(string[] args, int startIndex, ConfigFileScope defaultScope)
    {
        for (int i = startIndex; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg.ToLower())
            {
                case "--global":
                case "-g":
                    return ConfigFileScope.Global;
                case "--user":
                case "-u":
                    return ConfigFileScope.User;
                case "--local":
                case "-l":
                    return ConfigFileScope.Local;
                case "--any":
                case "-a":
                    return ConfigFileScope.Any;
            }
        }
        return defaultScope;
    }

    private void DisplayConfigValueWithLocation(StringWriter output, string key, ConfigValue value)
    {
        // Get location display name based on the config value source
        var location = value.Source switch
        {
            ConfigSource.CommandLine => "Command line (specified)",
            ConfigSource.EnvironmentVariable => "Environment variable (specified)",
            ConfigSource.ConfigFileName => $"{value.File?.FileName} (specified)",
            ConfigSource.LocalConfig => $"{value.File?.FileName} (local)",
            ConfigSource.UserConfig => $"{value.File?.FileName} (user)",
            ConfigSource.GlobalConfig => $"{value.File?.FileName} (global)",
            _ => "Unknown location"
        };

        // Write location header
        output.WriteLine($"{location}:");

        // Display the config value with indentation
        DisplayConfigValue(output, key, value, 2);
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