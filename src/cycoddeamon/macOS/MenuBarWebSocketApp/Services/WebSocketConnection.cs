using System;
using System.Net.WebSockets;

namespace MenuBarWebSocketApp.Services;

public class WebSocketConnection
{
    public string Id { get; set; } = "";
    public WebSocket Socket { get; set; } = null!;
    public string Origin { get; set; } = "";
    public bool IsAuthenticated { get; set; }
    public bool IsStarted { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public int MessageCount { get; set; }
}