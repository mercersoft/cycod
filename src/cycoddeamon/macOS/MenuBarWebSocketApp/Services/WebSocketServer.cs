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

public enum ServerState
{
    Stopped,        // Server not running
    Running,        // Server running, no connections
    Connected,      // Server running with connections, no start command
    Started,        // Server running with connections and start command received
    Error          // Error occurred
}

public class WebSocketServer
{
    private WebApplication? _app;
    private readonly Dictionary<string, WebSocketConnection> _connections = new();
    private readonly object _lock = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly CommandExecutionService _commandExecutionService = new();
    
    public int Port { get; set; } = 6464;
    public string AllowedOrigin { get; set; } = "https://example.com,http://localhost:5173";
    public string AuthToken { get; set; } = "";
    public bool IsRunning { get; private set; }
    public int ActiveConnections => _connections.Count(c => c.Value.IsAuthenticated);
    public ServerState CurrentState { get; private set; } = ServerState.Stopped;
    
    public event Action<string>? OnActivityLogged;
    public event Action<ServerState>? OnStateChanged;

    public Task StartAsync()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://localhost:{Port}");
        
        _app = builder.Build();
        
        _app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromSeconds(30)
        });
        
        _app.Map("/ws", HandleWebSocket);
        
        _app.MapGet("/health", () => new { status = "running", connections = ActiveConnections });
        
        _ = Task.Run(async () => 
        {
            try
            {
                await _app.RunAsync();
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        });
        IsRunning = true;
        UpdateState();
        
        LogActivity($"Server started on ws://localhost:{Port}");
        
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_app != null)
        {
            foreach (var connection in _connections.Values.ToList())
            {
                await CloseConnection(connection, "Server shutting down");
            }
            
            _cancellationTokenSource?.Cancel();
            await _app.StopAsync();
            await _app.DisposeAsync();
            _app = null;
        }
        
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        
        // Cleanup command execution service
        _commandExecutionService?.Dispose();
        
        IsRunning = false;
        UpdateState();
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
        Console.WriteLine($"[CONNECTION] New WebSocket connection: {connectionId} from {origin}");
        UpdateState(); // Update state when new connection is added
        
        try
        {
            await SendMessage(connection, new
            {
                type = "challenge",
                connectionId = connection.Id,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });
            
            await HandleConnection(connection);
        }
        finally
        {
            lock (_lock)
            {
                _connections.Remove(connectionId);
            }
            Console.WriteLine($"[CONNECTION] WebSocket connection closed: {connectionId}");
            UpdateState(); // Update state when connection is removed
            LogActivity($"Connection closed: {connectionId}");
        }
    }

    private async Task HandleConnection(WebSocketConnection connection)
    {
        var buffer = new ArraySegment<byte>(new byte[4096]);
        
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
                Console.WriteLine($"[AUTH] Authentication attempt from {connection.Id} with token: '{token}'");
                
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
                    Console.WriteLine($"[AUTH] Authentication successful for {connection.Id}");
                    UpdateState(); // Update state when authentication changes
                }
                else
                {
                    await SendMessage(connection, new
                    {
                        type = "error",
                        message = "Invalid authentication"
                    });
                    Console.WriteLine($"[AUTH] Authentication failed for {connection.Id}");
                    await CloseConnection(connection, "Authentication failed");
                }
                return;
            }
            
            if (!connection.IsAuthenticated)
            {
                await CloseConnection(connection, "Not authenticated");
                return;
            }
            
            connection.LastActivityAt = DateTime.UtcNow;
            
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
                    
                case "version":
                    await SendMessage(connection, new
                    {
                        type = "version",
                        version = "CycoDev Deamon 1.0.0"
                    });
                    break;
                    
                case "start":
                    Console.WriteLine($"[START COMMAND] Received from connection {connection.Id}");
                    connection.IsStarted = true;
                    UpdateState();
                    await SendMessage(connection, new
                    {
                        type = "started",
                        message = "Server started successfully"
                    });
                    LogActivity("Start command received");
                    Console.WriteLine($"[START COMMAND] Response sent, connection.IsStarted = {connection.IsStarted}");
                    break;
                    
                case "stop":
                    Console.WriteLine($"[STOP COMMAND] Received from connection {connection.Id}");
                    connection.IsStarted = false;
                    UpdateState();
                    await SendMessage(connection, new
                    {
                        type = "stopped",
                        message = "Server stopped successfully"
                    });
                    LogActivity("Stop command received");
                    Console.WriteLine($"[STOP COMMAND] Response sent, connection.IsStarted = {connection.IsStarted}");
                    break;
                    
                case "command":
                    await HandleCommandMessage(connection, json);
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

    private async Task HandleCommandMessage(WebSocketConnection connection, JsonElement json)
    {
        try
        {
            // Extract command details
            var command = json.GetProperty("command").GetString() ?? "";
            var requestId = json.GetProperty("requestId").GetString() ?? "";
            
            // Extract args array
            var argsProperty = json.GetProperty("args");
            var args = new List<string>();
            
            if (argsProperty.ValueKind == JsonValueKind.Array)
            {
                foreach (var arg in argsProperty.EnumerateArray())
                {
                    if (arg.ValueKind == JsonValueKind.String)
                    {
                        args.Add(arg.GetString() ?? "");
                    }
                }
            }

            Console.WriteLine($"[COMMAND] Executing: {command} {string.Join(" ", args)}");
            LogActivity($"Executing command: {command} {string.Join(" ", args)}");

            // Execute the command
            var result = await _commandExecutionService.ExecuteCommandAsync(command, args.ToArray());

            // Send the result back
            await SendMessage(connection, new
            {
                type = "command-result",
                requestId = requestId,
                stdout = result.StandardOutput,
                stderr = result.StandardError,
                exitCode = result.ExitCode
            });

            Console.WriteLine($"[COMMAND] Completed with exit code: {result.ExitCode}");
            LogActivity($"Command completed with exit code: {result.ExitCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[COMMAND] Error processing command: {ex.Message}");
            LogActivity($"Command processing error: {ex.Message}");
            
            // Try to get requestId for error response
            var requestId = "";
            try
            {
                requestId = json.GetProperty("requestId").GetString() ?? "";
            }
            catch { /* ignore */ }

            await SendMessage(connection, new
            {
                type = "command-result",
                requestId = requestId,
                stdout = "",
                stderr = $"Command execution error: {ex.Message}",
                exitCode = -1
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
            return true;
        
        return token == AuthToken;
    }

    private void LogActivity(string message)
    {
        OnActivityLogged?.Invoke(message);
    }

    private void UpdateState()
    {
        var previousState = CurrentState;
        
        if (!IsRunning)
        {
            CurrentState = ServerState.Stopped;
        }
        else if (ActiveConnections == 0)
        {
            CurrentState = ServerState.Running;
        }
        else
        {
            // Check if any connection has received a start command
            bool hasStartedConnection = _connections.Values
                .Any(c => c.IsAuthenticated && c.IsStarted);
                
            CurrentState = hasStartedConnection ? ServerState.Started : ServerState.Connected;
        }
        
        // Debug output for state tracking
        Console.WriteLine($"[DEBUG] UpdateState: Running={IsRunning}, ActiveConnections={ActiveConnections}, CurrentState={CurrentState}");
        
        // Notify if state changed
        if (previousState != CurrentState)
        {
            Console.WriteLine($"[STATE CHANGE] {previousState} -> {CurrentState}");
            OnStateChanged?.Invoke(CurrentState);
            LogActivity($"State changed from {previousState} to {CurrentState}");
        }
    }

    public async Task BroadcastToAll(object message)
    {
        var tasks = _connections.Values
            .Where(c => c.IsAuthenticated && c.Socket.State == WebSocketState.Open)
            .Select(c => SendMessage(c, message));
        
        await Task.WhenAll(tasks);
    }
}