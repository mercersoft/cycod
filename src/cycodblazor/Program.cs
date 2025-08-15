using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Cycodlib.Abstractions;
using Cycodblazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register dependency injection services for Blazor application
builder.Services.AddSingleton<Cycodlib.Abstractions.ILogger>(provider =>
{
    var jsRuntime = provider.GetRequiredService<Microsoft.JSInterop.IJSRuntime>();
    return new BlazorLogger(jsRuntime, isDebugEnabled: true, isQuietMode: false);
});

builder.Services.AddSingleton<IStorageProvider>(provider =>
{
    var jsRuntime = provider.GetRequiredService<Microsoft.JSInterop.IJSRuntime>();
    return new BrowserStorageProvider(jsRuntime);
});

builder.Services.AddSingleton<Cycodlib.Abstractions.IConfigurationProvider, BlazorConfigurationProvider>();

// Register ChatService with proper DI
builder.Services.AddSingleton<cycodblazor.Services.ChatService>();

// Note: IShellExecutor is not available in Blazor WebAssembly due to security restrictions

// Build and configure the application
var app = builder.Build();

// Set up service provider for static API access
cycodblazor.Api.SetServiceProvider(app.Services);

// No root components registered — this stays headless.
await app.RunAsync();
