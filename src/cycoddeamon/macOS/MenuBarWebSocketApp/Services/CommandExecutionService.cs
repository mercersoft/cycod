using System;
using System.Diagnostics;
using System.IO;
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
        
        Console.WriteLine($"[COMMAND EXEC] Results - ExitCode: {exitCode}, TimedOut: {timedOut}");
        Console.WriteLine($"[COMMAND EXEC] Stdout length: {stdoutContent.Length}");
        Console.WriteLine($"[COMMAND EXEC] Stderr length: {stderrContent.Length}");
        if (!string.IsNullOrEmpty(stdoutContent))
        {
            Console.WriteLine($"[COMMAND EXEC] Stdout content: {stdoutContent}");
        }
        if (!string.IsNullOrEmpty(stderrContent))
        {
            Console.WriteLine($"[COMMAND EXEC] Stderr content: {stderrContent}");
        }

        var result = new CommandResult
        {
            StandardOutput = stdoutContent,
            StandardError = stderrContent,
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

    private Task<int> ProcessConfigCommandDirectly(string[] args, StringWriter output)
    {
        try
        {
            // Simple direct implementation for config commands
            if (args.Length < 2 || args[0] != "config")
            {
                output.WriteLine("Only config commands are supported in direct mode");
                return Task.FromResult(1);
            }

            var configProvider = _serviceProvider.GetService<Cycodlib.Abstractions.IConfigurationProvider>();
            if (configProvider == null)
            {
                output.WriteLine("Configuration provider not available");
                return Task.FromResult(1);
            }

            var subcommand = args[1].ToLower();
            
            switch (subcommand)
            {
                case "list":
                    // Simple config list implementation
                    output.WriteLine("LOCATION: (direct mode - limited functionality)");
                    output.WriteLine("  Direct mode config access - limited functionality");
                    output.WriteLine("  Use full cycod installation for complete config management");
                    return Task.FromResult(0);

                case "get":
                    if (args.Length < 3)
                    {
                        output.WriteLine("Usage: config get <key>");
                        return Task.FromResult(1);
                    }
                    var value = configProvider.GetConfigValue(args[2]);
                    if (value != null)
                    {
                        output.WriteLine($"{args[2]}: {value}");
                        return Task.FromResult(0);
                    }
                    else
                    {
                        output.WriteLine($"Configuration key '{args[2]}' not found");
                        return Task.FromResult(1);
                    }

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

    public void Dispose()
    {
        if (_serviceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }
    }
}