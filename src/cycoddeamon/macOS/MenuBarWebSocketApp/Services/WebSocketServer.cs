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
    private CancellationTokenSource? _cancellationTokenSource;
    
    public int Port { get; set; } = 6464;
    public string AllowedOrigin { get; set; } = "https://example.com";
    public string AuthToken { get; set; } = "";
    public bool IsRunning { get; private set; }
    public int ActiveConnections => _connections.Count(c => c.Value.IsAuthenticated);
    
    public event Action<string>? OnActivityLogged;

    public async Task StartAsync()
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
        
        LogActivity($"Server started on ws://localhost:{Port}");
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
            return true;
        
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