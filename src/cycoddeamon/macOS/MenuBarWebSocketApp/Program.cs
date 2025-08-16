using Avalonia;
using System;

namespace MenuBarWebSocketApp;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Handle Ctrl+C gracefully
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; // Don't terminate immediately
            Environment.Exit(0);
        };

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}