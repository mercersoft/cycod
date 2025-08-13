using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Cycodlib.Abstractions;
using Cycodblazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register dependency injection services for Blazor application
builder.Services.AddSingleton<ILogger>(provider =>
{
    var jsRuntime = provider.GetRequiredService<Microsoft.JSInterop.IJSRuntime>();
    return new BlazorLogger(jsRuntime, isDebugEnabled: true, isQuietMode: false);
});

builder.Services.AddSingleton<IStorageProvider>(provider =>
{
    var jsRuntime = provider.GetRequiredService<Microsoft.JSInterop.IJSRuntime>();
    return new BrowserStorageProvider(jsRuntime);
});

builder.Services.AddSingleton<IConfigurationProvider, BlazorConfigurationProvider>();

// Note: IShellExecutor is not available in Blazor WebAssembly due to security restrictions

// No root components registered — this stays headless.
await builder.Build().RunAsync();
