using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Cycodlib.Abstractions;
using Cycod.Implementation;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Setup dependency injection for console application
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                // Register platform-specific implementations
                services.AddSingleton<Cycodlib.Abstractions.ILogger>(provider => 
                {
                    var config = provider.GetService<Cycodlib.Abstractions.IConfigurationProvider>();
                    var isDebugEnabled = config?.GetConfigValue("debug") == "true";
                    var isQuietMode = config?.GetConfigValue("quiet") == "true";
                    return ConsoleLogger.ForComponent("Console", !isQuietMode);
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
            })
            .Build();
            
        // Pass the service provider to the existing runner
        using var scope = host.Services.CreateScope();
        return await CycoDevProgramRunner.RunAsync(args, scope.ServiceProvider);
    }
}
