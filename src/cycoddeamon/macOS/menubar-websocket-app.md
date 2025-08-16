# macOS Menu Bar WebSocket App with Avalonia UI

## Project Overview
Create a macOS menu bar application using Avalonia UI and .NET that:
- Runs as a menu bar item in the top-right corner of macOS
- Hosts a WebSocket server on localhost
- Shows a popup window with configuration UI when clicked
- Allows secure communication with a website via WebSockets

## Project Setup

### 1. Create the Project
```bash
# Install Avalonia templates
dotnet new install Avalonia.Templates

# Create new Avalonia application
dotnet new avalonia.app -n MenuBarWebSocketApp

# Navigate to project
cd MenuBarWebSocketApp

# Add required NuGet packages
dotnet add package Microsoft.AspNetCore.App
dotnet add package System.Text.Json
```

### 2. Project Structure
```
MenuBarWebSocketApp/
├── App.axaml
├── App.axaml.cs
├── Program.cs
├── Assets/
│   └── icon.png (16x16 or 32x32 menu bar icon)
├── Views/
│   └── PopupWindow.axaml
│   └── PopupWindow.axaml.cs
├── ViewModels/
│   └── PopupViewModel.cs
├── Services/
│   └── WebSocketServer.cs
│   └── WebSocketConnection.cs
└── MenuBarWebSocketApp.csproj
```

## Implementation Files

### Program.cs
```csharp
using Avalonia;
using System;

namespace MenuBarWebSocketApp;

class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
```

### App.axaml.cs
```csharp
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
        // Initialize and start WebSocket server
        _webSocketServer = new WebSocketServer();
        await StartWebSocketServer();

        // Create tray icon
        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://MenuBarWebSocketApp/Assets/icon.png"))),
            ToolTipText = "WebSocket Server",
            IsVisible = true
        };

        // Handle tray icon click
        _trayIcon.Clicked += OnTrayIconClicked;

        // Create context menu for right-click
        var menu = new NativeMenu();
        var showItem = new NativeMenuItem("Show Configuration");
        var quitItem = new NativeMenuItem("Quit");
        
        showItem.Click += OnTrayIconClicked;
        quitItem.Click += OnQuitClicked;
        
        menu.Add(showItem);
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(quitItem);
        _trayIcon.Menu = menu;

        // Keep app running without main window
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
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
            // Log error and update icon
            Console.WriteLine($"Failed to start WebSocket server: {ex.Message}");
            UpdateTrayIcon(false);
        }
    }

    private void UpdateTrayIcon(bool isRunning)
    {
        if (_trayIcon != null)
        {
            _trayIcon.ToolTipText = isRunning 
                ? "WebSocket Server (Running on ws://localhost:1234)" 
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

            // Position near menu bar
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
        _webSocketServer?.StopAsync().Wait();
        _trayIcon?.Dispose();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
```

### App.axaml
```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="MenuBarWebSocketApp.App"
             RequestedThemeVariant="Default">
    <Application.Styles>
        <FluentTheme />
    </Application.Styles>
</Application>
```

### Views/PopupWindow.axaml
```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:vm="using:MenuBarWebSocketApp.ViewModels"
        mc:Ignorable="d"
        x:Class="MenuBarWebSocketApp.Views.PopupWindow"
        Title="WebSocket Server Configuration"
        Width="450" Height="350"
        CanResize="False"
        SystemDecorations="None"
        TransparencyLevelHint="Transparent"
        Background="Transparent"
        WindowStartupLocation="Manual">
    
    <Border CornerRadius="10" 
            Background="{DynamicResource SystemRegionBrush}"
            BoxShadow="0 2 10 0 #40000000"
            Margin="10">
        
        <Grid RowDefinitions="Auto,*,Auto" Margin="20">
            <!-- Header -->
            <Grid Grid.Row="0" ColumnDefinitions="*,Auto" Margin="0,0,0,20">
                <TextBlock Text="WebSocket Server Configuration" 
                          FontSize="18" 
                          FontWeight="Bold"
                          VerticalAlignment="Center"/>
                <TextBlock Grid.Column="1" 
                          Text="{Binding StatusText}"
                          Foreground="{Binding StatusColor}"
                          VerticalAlignment="Center"
                          FontWeight="SemiBold"/>
            </Grid>
            
            <!-- Content Area -->
            <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto">
                <StackPanel Spacing="15">
                    <!-- Server Info -->
                    <Border Background="{DynamicResource SystemControlBackgroundBaseLowBrush}" 
                           CornerRadius="5"
                           Padding="10">
                        <StackPanel Spacing="8">
                            <TextBlock Text="Server Information" FontWeight="SemiBold"/>
                            <Grid ColumnDefinitions="120,*" RowDefinitions="Auto,Auto,Auto" RowSpacing="5">
                                <TextBlock Grid.Row="0" Grid.Column="0" Text="Status:"/>
                                <TextBlock Grid.Row="0" Grid.Column="1" Text="{Binding ServerStatus}" FontWeight="SemiBold"/>
                                
                                <TextBlock Grid.Row="1" Grid.Column="0" Text="URL:"/>
                                <TextBlock Grid.Row="1" Grid.Column="1" Text="ws://localhost:1234" FontFamily="Courier New"/>
                                
                                <TextBlock Grid.Row="2" Grid.Column="0" Text="Active Connections:"/>
                                <TextBlock Grid.Row="2" Grid.Column="1" Text="{Binding ActiveConnections}"/>
                            </Grid>
                        </StackPanel>
                    </Border>
                    
                    <!-- Configuration Section -->
                    <Border Background="{DynamicResource SystemControlBackgroundBaseLowBrush}" 
                           CornerRadius="5"
                           Padding="10">
                        <StackPanel Spacing="10">
                            <TextBlock Text="Configuration" FontWeight="SemiBold" Margin="0,0,0,5"/>
                            
                            <!-- Port Configuration -->
                            <Grid ColumnDefinitions="120,*" RowSpacing="10">
                                <TextBlock Grid.Column="0" Text="Port:" VerticalAlignment="Center"/>
                                <TextBox Grid.Column="1" 
                                        Text="{Binding Port}" 
                                        Watermark="1234"
                                        IsEnabled="{Binding !IsServerRunning}"/>
                            </Grid>
                            
                            <!-- Allowed Origin -->
                            <Grid ColumnDefinitions="120,*" RowSpacing="10">
                                <TextBlock Grid.Column="0" Text="Allowed Origin:" VerticalAlignment="Center"/>
                                <TextBox Grid.Column="1" 
                                        Text="{Binding AllowedOrigin}" 
                                        Watermark="https://example.com"/>
                            </Grid>
                            
                            <!-- Authentication Token -->
                            <Grid ColumnDefinitions="120,*" RowSpacing="10">
                                <TextBlock Grid.Column="0" Text="Auth Token:" VerticalAlignment="Center"/>
                                <TextBox Grid.Column="1" 
                                        Text="{Binding AuthToken}" 
                                        PasswordChar="•"
                                        Watermark="Shared secret token"/>
                            </Grid>
                            
                            <!-- Auto-start Option -->
                            <CheckBox Content="Start server automatically on launch" 
                                     IsChecked="{Binding AutoStart}"/>
                        </StackPanel>
                    </Border>
                    
                    <!-- Connection Log -->
                    <Border Background="{DynamicResource SystemControlBackgroundBaseLowBrush}" 
                           CornerRadius="5"
                           Padding="10">
                        <StackPanel Spacing="5">
                            <TextBlock Text="Recent Activity" FontWeight="SemiBold"/>
                            <TextBox Text="{Binding ActivityLog}" 
                                    IsReadOnly="True"
                                    Height="60"
                                    TextWrapping="Wrap"
                                    FontFamily="Courier New"
                                    FontSize="11"/>
                        </StackPanel>
                    </Border>
                </StackPanel>
            </ScrollViewer>
            
            <!-- Button Bar -->
            <Grid Grid.Row="2" ColumnDefinitions="*,Auto,Auto" Margin="0,20,0,0">
                <Button Grid.Column="1" 
                       Content="OK" 
                       Command="{Binding SaveCommand}"
                       Width="80"
                       Margin="0,0,10,0"
                       HotKey="Enter"/>
                <Button Grid.Column="2" 
                       Content="Cancel" 
                       Command="{Binding CancelCommand}"
                       Width="80"
                       HotKey="Escape"/>
            </Grid>
        </Grid>
    </Border>
</Window>
```

### Views/PopupWindow.axaml.cs
```csharp
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MenuBarWebSocketApp.ViewModels;
using System;

namespace MenuBarWebSocketApp.Views;

public partial class PopupWindow : Window
{
    public PopupWindow()
    {
        InitializeComponent();
        
        // Close when clicking outside
        Deactivated += (s, e) => Close();
        
        // Handle Escape key
        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape)
                Close();
        };
    }
    
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        if (DataContext is PopupViewModel vm)
        {
            vm.CloseRequested += () => Close();
        }
    }
}
```

### ViewModels/PopupViewModel.cs
```csharp
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media;
using MenuBarWebSocketApp.Services;

namespace MenuBarWebSocketApp.ViewModels;

public class PopupViewModel : INotifyPropertyChanged
{
    private readonly WebSocketServer _server;
    private string _port = "1234";
    private string _allowedOrigin = "https://example.com";
    private string _authToken = "";
    private bool _autoStart = true;
    private string _activityLog = "";

    public PopupViewModel(WebSocketServer server)
    {
        _server = server;
        _server.OnActivityLogged += (message) => 
        {
            ActivityLog = $"[{DateTime.Now:HH:mm:ss}] {message}\n" + ActivityLog;
            if (ActivityLog.Length > 500)
                ActivityLog = ActivityLog.Substring(0, 500);
        };
        
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
        
        LoadCurrentSettings();
    }

    public string Port
    {
        get => _port;
        set { _port = value; OnPropertyChanged(); }
    }

    public string AllowedOrigin
    {
        get => _allowedOrigin;
        set { _allowedOrigin = value; OnPropertyChanged(); }
    }

    public string AuthToken
    {
        get => _authToken;
        set { _authToken = value; OnPropertyChanged(); }
    }

    public bool AutoStart
    {
        get => _autoStart;
        set { _autoStart = value; OnPropertyChanged(); }
    }

    public string ActivityLog
    {
        get => _activityLog;
        set { _activityLog = value; OnPropertyChanged(); }
    }

    public bool IsServerRunning => _server.IsRunning;
    public string ServerStatus => IsServerRunning ? "Running" : "Stopped";
    public string StatusText => IsServerRunning ? "● Running" : "● Stopped";
    public IBrush StatusColor => IsServerRunning ? Brushes.Green : Brushes.Gray;
    public int ActiveConnections => _server.ActiveConnections;

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    
    public event Action? CloseRequested;

    private void LoadCurrentSettings()
    {
        Port = _server.Port.ToString();
        AllowedOrigin = _server.AllowedOrigin;
        AuthToken = _server.AuthToken;
        // Load AutoStart from preferences
    }

    private async void Save()
    {
        // Validate and apply settings
        if (int.TryParse(Port, out var portNumber))
        {
            _server.Port = portNumber;
        }
        
        _server.AllowedOrigin = AllowedOrigin;
        _server.AuthToken = AuthToken;
        
        // Save AutoStart to preferences
        
        // Restart server if settings changed
        if (_server.IsRunning)
        {
            await _server.RestartAsync();
        }
        
        CloseRequested?.Invoke();
    }

    private void Cancel()
    {
        CloseRequested?.Invoke();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
}
```

### Services/WebSocketServer.cs
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MenuBarWebSocketApp.Services;

public class WebSocketServer
{
    private WebApplication? _app;
    private readonly Dictionary<string, WebSocketConnection> _connections = new();
    private readonly object _lock = new();
    
    public int Port { get; set; } = 1234;
    public string AllowedOrigin { get; set; } = "https://example.com";
    public string AuthToken { get; set; } = "";
    public bool IsRunning { get; private set; }
    public int ActiveConnections => _connections.Count(c => c.Value.IsAuthenticated);
    
    public event Action<string>? OnActivityLogged;

    public async Task StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://localhost:{Port}");
        
        _app = builder.Build();
        
        _app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromSeconds(30)
        });
        
        _app.Map("/ws", HandleWebSocket);
        
        // Health check endpoint
        _app.MapGet("/health", () => new { status = "running", connections = ActiveConnections });
        
        _ = Task.Run(async () => await _app.RunAsync());
        IsRunning = true;
        
        LogActivity($"Server started on ws://localhost:{Port}");
    }

    public async Task StopAsync()
    {
        if (_app != null)
        {
            // Close all connections
            foreach (var connection in _connections.Values.ToList())
            {
                await CloseConnection(connection, "Server shutting down");
            }
            
            await _app.StopAsync();
            await _app.DisposeAsync();
            _app = null;
        }
        
        IsRunning = false;
        LogActivity("Server stopped");
    }

    public async Task RestartAsync()
    {
        await StopAsync();
        await StartAsync();
    }

    private async Task HandleWebSocket(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }
        
        // Validate origin
        var origin = context.Request.Headers["Origin"].ToString();
        if (!IsOriginAllowed(origin))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Forbidden origin");
            LogActivity($"Rejected connection from {origin}");
            return;
        }
        
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var connectionId = Guid.NewGuid().ToString();
        
        var connection = new WebSocketConnection
        {
            Id = connectionId,
            Socket = webSocket,
            Origin = origin,
            ConnectedAt = DateTime.UtcNow
        };
        
        lock (_lock)
        {
            _connections[connectionId] = connection;
        }
        
        LogActivity($"New connection from {origin}");
        
        try
        {
            // Send challenge
            await SendMessage(connection, new
            {
                type = "challenge",
                connectionId = connection.Id,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });
            
            // Handle connection
            await HandleConnection(connection);
        }
        finally
        {
            lock (_lock)
            {
                _connections.Remove(connectionId);
            }
            LogActivity($"Connection closed: {connectionId}");
        }
    }

    private async Task HandleConnection(WebSocketConnection connection)
    {
        var buffer = new ArraySegment<byte>(new byte[4096]);
        
        // Set authentication timeout
        var authTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        
        try
        {
            while (connection.Socket.State == WebSocketState.Open)
            {
                var result = await connection.Socket.ReceiveAsync(buffer, CancellationToken.None);
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer.Array!, 0, result.Count);
                    await ProcessMessage(connection, message);
                    
                    // Cancel auth timeout if authenticated
                    if (connection.IsAuthenticated && !authTimeout.Token.IsCancellationRequested)
                    {
                        authTimeout.Cancel();
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await connection.Socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closing",
                        CancellationToken.None);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Authentication timeout
            if (!connection.IsAuthenticated)
            {
                await CloseConnection(connection, "Authentication timeout");
            }
        }
        catch (WebSocketException ex)
        {
            LogActivity($"WebSocket error: {ex.Message}");
        }
    }

    private async Task ProcessMessage(WebSocketConnection connection, string message)
    {
        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(message);
            var type = json.GetProperty("type").GetString();
            
            if (type == "authenticate" && !connection.IsAuthenticated)
            {
                var token = json.GetProperty("token").GetString();
                
                if (ValidateToken(token, connection.Id))
                {
                    connection.IsAuthenticated = true;
                    connection.LastActivityAt = DateTime.UtcNow;
                    
                    await SendMessage(connection, new
                    {
                        type = "authenticated",
                        status = "success"
                    });
                    
                    LogActivity($"Connection authenticated: {connection.Id}");
                }
                else
                {
                    await SendMessage(connection, new
                    {
                        type = "error",
                        message = "Invalid authentication"
                    });
                    await CloseConnection(connection, "Authentication failed");
                }
                return;
            }
            
            if (!connection.IsAuthenticated)
            {
                await CloseConnection(connection, "Not authenticated");
                return;
            }
            
            // Update activity
            connection.LastActivityAt = DateTime.UtcNow;
            
            // Handle authenticated messages
            switch (type)
            {
                case "ping":
                    await SendMessage(connection, new { type = "pong" });
                    break;
                    
                case "getData":
                    await SendMessage(connection, new
                    {
                        type = "data",
                        data = new
                        {
                            serverTime = DateTime.UtcNow,
                            connections = ActiveConnections,
                            uptime = DateTime.UtcNow - connection.ConnectedAt
                        }
                    });
                    break;
                    
                case "echo":
                    var content = json.GetProperty("content").GetString();
                    await SendMessage(connection, new
                    {
                        type = "echo",
                        content = content
                    });
                    LogActivity($"Echo: {content}");
                    break;
            }
        }
        catch (Exception ex)
        {
            LogActivity($"Message processing error: {ex.Message}");
            await SendMessage(connection, new
            {
                type = "error",
                message = "Invalid message format"
            });
        }
    }

    private async Task SendMessage(WebSocketConnection connection, object message)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        
        await connection.Socket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }

    private async Task CloseConnection(WebSocketConnection connection, string reason)
    {
        if (connection.Socket.State == WebSocketState.Open)
        {
            await connection.Socket.CloseAsync(
                WebSocketCloseStatus.PolicyViolation,
                reason,
                CancellationToken.None);
        }
    }

    private bool IsOriginAllowed(string origin)
    {
        if (string.IsNullOrEmpty(AllowedOrigin))
            return true;
        
        var allowedOrigins = AllowedOrigin.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(o => o.Trim());
        
        return allowedOrigins.Contains(origin);
    }

    private bool ValidateToken(string? token, string connectionId)
    {
        if (string.IsNullOrEmpty(AuthToken))
            return true; // No auth required if no token set
        
        // Implement your token validation logic
        // This is a simple example - use proper crypto in production
        return token == AuthToken;
    }

    private void LogActivity(string message)
    {
        OnActivityLogged?.Invoke(message);
    }

    public async Task BroadcastToAll(object message)
    {
        var tasks = _connections.Values
            .Where(c => c.IsAuthenticated && c.Socket.State == WebSocketState.Open)
            .Select(c => SendMessage(c, message));
        
        await Task.WhenAll(tasks);
    }
}
```

### Services/WebSocketConnection.cs
```csharp
using System;
using System.Net.WebSockets;

namespace MenuBarWebSocketApp.Services;

public class WebSocketConnection
{
    public string Id { get; set; } = "";
    public WebSocket Socket { get; set; } = null!;
    public string Origin { get; set; } = "";
    public bool IsAuthenticated { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public int MessageCount { get; set; }
}
```

### JavaScript Client Example (for the website)
```javascript
// WebSocketClient.js - Include this on your website
class LocalAppWebSocketClient {
    constructor(authToken) {
        this.ws = null;
        this.authToken = authToken;
        this.isAuthenticated = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.messageHandlers = new Map();
    }
    
    async connect() {
        return new Promise((resolve, reject) => {
            try {
                this.ws = new WebSocket('ws://localhost:1234/ws');
                
                this.ws.onopen = () => {
                    console.log('Connected to local app');
                    this.reconnectAttempts = 0;
                    resolve();
                };
                
                this.ws.onmessage = async (event) => {
                    const message = JSON.parse(event.data);
                    await this.handleMessage(message);
                };
                
                this.ws.onerror = (error) => {
                    console.error('WebSocket error:', error);
                    reject(error);
                };
                
                this.ws.onclose = () => {
                    console.log('Disconnected from local app');
                    this.isAuthenticated = false;
                    this.attemptReconnect();
                };
                
            } catch (error) {
                reject(error);
            }
        });
    }
    
    async handleMessage(message) {
        switch (message.type) {
            case 'challenge':
                // Respond with authentication
                this.send({
                    type: 'authenticate',
                    token: this.authToken
                });
                break;
                
            case 'authenticated':
                this.isAuthenticated = true;
                console.log('Authenticated successfully');
                if (this.messageHandlers.has('authenticated')) {
                    this.messageHandlers.get('authenticated')();
                }
                break;
                
            case 'error':
                console.error('Server error:', message.message);
                break;
                
            default:
                if (this.messageHandlers.has(message.type)) {
                    this.messageHandlers.get(message.type)(message);
                }
        }
    }
    
    send(message) {
        if (this.ws && this.ws.readyState === WebSocket.OPEN) {
            this.ws.send(JSON.stringify(message));
        }
    }
    
    on(event, handler) {
        this.messageHandlers.set(event, handler);
    }
    
    getData() {
        this.send({ type: 'getData' });
    }
    
    echo(content) {
        this.send({ type: 'echo', content });
    }
    
    attemptReconnect() {
        if (this.reconnectAttempts >= this.maxReconnectAttempts) {
            console.error('Max reconnection attempts reached');
            return;
        }
        
        this.reconnectAttempts++;
        const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts), 10000);
        
        setTimeout(() => {
            console.log(`Reconnection attempt ${this.reconnectAttempts}`);
            this.connect();
        }, delay);
    }
    
    close() {
        if (this.ws) {
            this.ws.close();
        }
    }
}

// Usage example
const client = new LocalAppWebSocketClient('your-shared-secret');

client.on('authenticated', () => {
    console.log('Ready to communicate');
    client.getData();
});

client.on('data', (message) => {
    console.log('Received data:', message.data);
});

client.on('echo', (message) => {
    console.log('Echo response:', message.content);
});

// Connect to the local app
client.connect().then(() => {
    console.log('WebSocket connected');
}).catch(error => {
    console.error('Failed to connect:', error);
    alert('Please ensure the desktop app is running');
});
```

## Build and Run Instructions

### 1. Build the Application
```bash
# Debug build
dotnet build

# Release build for macOS
dotnet publish -c Release -r osx-x64 --self-contained
```

### 2. Create macOS App Bundle (Optional)
Create an `.app` bundle for better macOS integration:
```bash
# Create app structure
mkdir -p MyApp.app/Contents/MacOS
mkdir -p MyApp.app/Contents/Resources

# Copy executable
cp bin/Release/net8.0/osx-x64/publish/MenuBarWebSocketApp MyApp.app/Contents/MacOS/

# Copy icon
cp Assets/icon.icns MyApp.app/Contents/Resources/

# Create Info.plist
cat > MyApp.app/Contents/Info.plist << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>MenuBarWebSocketApp</string>
    <key>CFBundleIdentifier</key>
    <string>com.yourcompany.menubarwebsocketapp</string>
    <key>CFBundleName</key>
    <string>MenuBar WebSocket App</string>
    <key>LSUIElement</key>
    <true/>
</dict>
</plist>
EOF
```

### 3. Run the Application
```bash
# From project directory
dotnet run

# Or run the published executable
./bin/Release/net8.0/osx-x64/publish/MenuBarWebSocketApp
```

## Security Considerations

1. **Origin Validation**: Always validate the Origin header to prevent unauthorized connections
2. **Authentication**: Implement proper token-based authentication
3. **Rate Limiting**: Limit messages per connection to prevent abuse
4. **Input Validation**: Validate all incoming messages
5. **HTTPS Alternative**: Consider using a self-signed certificate for `wss://` if needed
6. **Connection Limits**: Limit total number of concurrent connections
7. **Activity Timeout**: Close inactive connections after a timeout period

## Testing

Test the WebSocket server using a tool like `wscat`:
```bash
# Install wscat
npm install -g wscat

# Connect to the server
wscat -c ws://localhost:1234/ws -H "Origin: https://example.com"

# Send authentication
{"type":"authenticate","token":"your-token"}

# Send test messages
{"type":"ping"}
{"type":"getData"}
{"type":"echo","content":"Hello World"}
```

## Troubleshooting

### Port Already in Use
If port 1234 is already in use, change it in the configuration UI or check what's using it:
```bash
lsof -i :1234
```

### Menu Bar Icon Not Showing
- Ensure `icon.png` exists in the Assets folder
- Check that the icon is included in the project file as an AvaloniaResource
- Icon should be 16x16 or 32x32 pixels

### WebSocket Connection Refused
- Verify the server is running (check the menu bar icon tooltip)
- Check firewall settings
- Ensure the origin is in the allowed list
- Check browser console for detailed error messages

## Next Steps

1. Add data persistence for configuration settings
2. Implement more sophisticated authentication (JWT, OAuth)
3. Add support for binary WebSocket messages
4. Create auto-update functionality
5. Add system notification support
6. Implement file transfer capabilities
7. Add encryption for sensitive data