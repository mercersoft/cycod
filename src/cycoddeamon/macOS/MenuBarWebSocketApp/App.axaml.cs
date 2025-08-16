using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using MenuBarWebSocketApp.Services;
using MenuBarWebSocketApp.Views;
using System;
using System.Threading.Tasks;

namespace MenuBarWebSocketApp;

public partial class App : Application
{
    private TrayIcon? _trayIcon;
    private PopupWindow? _popupWindow;
    private WebSocketServer? _webSocketServer;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        _webSocketServer = new WebSocketServer();
        await StartWebSocketServer();

        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://MenuBarWebSocketApp/Assets/icon.png"))),
            ToolTipText = "WebSocket Server",
            IsVisible = true
        };

        _trayIcon.Clicked += OnTrayIconClicked;

        var menu = new NativeMenu();
        var showItem = new NativeMenuItem("Show Configuration");
        var quitItem = new NativeMenuItem("Quit");
        
        showItem.Click += OnTrayIconClicked;
        quitItem.Click += OnQuitClicked;
        
        menu.Add(showItem);
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(quitItem);
        _trayIcon.Menu = menu;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.ShutdownRequested += OnShutdownRequested;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task StartWebSocketServer()
    {
        try
        {
            await _webSocketServer!.StartAsync();
            UpdateTrayIcon(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start WebSocket server: {ex.Message}");
            UpdateTrayIcon(false);
        }
    }

    private void UpdateTrayIcon(bool isRunning)
    {
        if (_trayIcon != null)
        {
            _trayIcon.ToolTipText = isRunning 
                ? $"WebSocket Server (Running on ws://localhost:{_webSocketServer?.Port ?? 6464})" 
                : "WebSocket Server (Stopped)";
        }
    }

    private void OnTrayIconClicked(object? sender, EventArgs e)
    {
        if (_popupWindow == null)
        {
            _popupWindow = new PopupWindow
            {
                DataContext = new ViewModels.PopupViewModel(_webSocketServer!)
            };

            _popupWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            
            if (_popupWindow.Screens.Primary is { } screen)
            {
                _popupWindow.Position = new PixelPoint(
                    screen.WorkingArea.Right - 450,
                    40
                );
            }

            _popupWindow.Show();
            _popupWindow.Closed += (s, e) => _popupWindow = null;
        }
        else
        {
            _popupWindow.Close();
        }
    }

    private void OnQuitClicked(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private async void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        e.Cancel = true; // Cancel the shutdown temporarily to allow cleanup
        
        try
        {
            await CleanupAsync();
        }
        finally
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown(0);
            }
        }
    }

    private async Task CleanupAsync()
    {
        if (_webSocketServer != null)
        {
            await _webSocketServer.StopAsync();
        }
        
        _trayIcon?.Dispose();
        _trayIcon = null;
        
        _popupWindow?.Close();
        _popupWindow = null;
    }
}