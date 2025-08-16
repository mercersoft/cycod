using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using MenuBarWebSocketApp.Services;
using MenuBarWebSocketApp.Views;
using System;
using System.Linq;
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
            
            // Subscribe to activity events to update icon when connections change
            _webSocketServer.OnActivityLogged += (message) => {
                // Dispatch UI updates to the main thread
                Dispatcher.UIThread.InvokeAsync(() => {
                    UpdateTrayIcon(_webSocketServer.CurrentState);
                });
            };
            
            // Subscribe to state changes for immediate icon updates
            _webSocketServer.OnStateChanged += (state) => {
                // Dispatch UI updates to the main thread
                Dispatcher.UIThread.InvokeAsync(() => {
                    UpdateTrayIcon(state);
                });
            };
            
            Console.WriteLine($"[STARTUP] WebSocket server started successfully");
            Console.WriteLine($"[STARTUP] Initial state: {_webSocketServer.CurrentState}");
            UpdateTrayIcon(_webSocketServer.CurrentState);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start WebSocket server: {ex.Message}");
            UpdateTrayIcon(ServerState.Error);
        }
    }

    private void UpdateTrayIcon(ServerState state)
    {
        if (_trayIcon != null)
        {
            // Update icon based on server state
            string iconPath = state switch
            {
                ServerState.Stopped => "avares://MenuBarWebSocketApp/Assets/icon.png",
                ServerState.Running => "avares://MenuBarWebSocketApp/Assets/icon.png", 
                ServerState.Connected => "avares://MenuBarWebSocketApp/Assets/icon-connected.png",
                ServerState.Started => "avares://MenuBarWebSocketApp/Assets/icon-running.png", // Use running icon when started
                ServerState.Error => "avares://MenuBarWebSocketApp/Assets/icon.png", // Use base icon for error
                _ => "avares://MenuBarWebSocketApp/Assets/icon.png"
            };
            
            // Debug output for icon updates
            Console.WriteLine($"[ICON UPDATE] State: {state}, Icon: {iconPath.Split('/').Last()}");
            
            try
            {
                // Force icon refresh by creating new WindowIcon instance
                var iconStream = AssetLoader.Open(new Uri(iconPath));
                var newIcon = new WindowIcon(iconStream);
                _trayIcon.Icon = newIcon;
                Console.WriteLine($"[ICON UPDATE] Successfully loaded icon: {iconPath.Split('/').Last()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ICON ERROR] Failed to load icon {iconPath}: {ex.Message}");
            }
            
            // Update tooltip with state information
            var (emoji, statusText, description) = state switch
            {
                ServerState.Stopped => ("🔴", "Stopped", "Server is not running"),
                ServerState.Running => ("⚪", "Running", "Server running, waiting for connections"),
                ServerState.Connected => ("🟡", "Connected", "Active connections, no start command"),
                ServerState.Started => ("🟢", "Started", "Active connections, start command received"),
                ServerState.Error => ("❌", "Error", "An error occurred"),
                _ => ("❓", "Unknown", "Unknown state")
            };
            
            _trayIcon.ToolTipText = $"{emoji} WebSocket Server - {statusText}\n" +
                                  $"Port: {_webSocketServer?.Port ?? 6464}\n" +
                                  $"Connections: {_webSocketServer?.ActiveConnections ?? 0}\n" +
                                  $"Status: {description}";
                                  
            Console.WriteLine($"[TOOLTIP] {emoji} {statusText} - Connections: {_webSocketServer?.ActiveConnections ?? 0}");
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