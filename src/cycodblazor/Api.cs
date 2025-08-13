using Microsoft.JSInterop;
using System.Text.Json;
using cycodblazor.Services;

namespace cycodblazor;

public static class Api
{
  private static ChatService? _chatService;

  // This method is callable from JS as DotNet.invokeMethodAsync('cycodheadless', 'Version')
  [JSInvokable(nameof(Version))]
  public static string Version() => "1.0.0 (Blazor WebAssembly)";

  // Initialize a new chat session
  [JSInvokable(nameof(InitializeChat))]
  public static async Task<string> InitializeChat(string systemPrompt, int? maxTokens = null)
  {
    try
    {
      _chatService ??= new ChatService();
      await _chatService.InitializeChatAsync(systemPrompt, maxTokens);
      return JsonSerializer.Serialize(new { success = true, message = "Chat initialized successfully" });
    }
    catch (Exception ex)
    {
      return JsonSerializer.Serialize(new { success = false, error = ex.Message });
    }
  }

  // Send a message and get complete response
  [JSInvokable(nameof(SendMessage))]
  public static async Task<string> SendMessage(string message)
  {
    try
    {
      if (_chatService == null)
        throw new InvalidOperationException("Chat service not initialized. Call InitializeChat first.");

      var response = await _chatService.SendMessageAsync(message);
      return JsonSerializer.Serialize(new { success = true, response });
    }
    catch (Exception ex)
    {
      return JsonSerializer.Serialize(new { success = false, error = ex.Message });
    }
  }

  // Send a message with streaming response (callback-based)
  [JSInvokable(nameof(SendMessageStreaming))]
  public static async Task<string> SendMessageStreaming(string message, DotNetObjectReference<StreamingCallback>? callback = null)
  {
    try
    {
      if (_chatService == null)
        throw new InvalidOperationException("Chat service not initialized. Call InitializeChat first.");

      await _chatService.SendMessageStreamingAsync(message, callback);
      return JsonSerializer.Serialize(new { success = true, message = "Streaming started" });
    }
    catch (Exception ex)
    {
      return JsonSerializer.Serialize(new { success = false, error = ex.Message });
    }
  }

  // Clear chat history
  [JSInvokable(nameof(ClearChatHistory))]
  public static string ClearChatHistory()
  {
    try
    {
      if (_chatService == null)
        throw new InvalidOperationException("Chat service not initialized. Call InitializeChat first.");

      _chatService.ClearChatHistory();
      return JsonSerializer.Serialize(new { success = true, message = "Chat history cleared" });
    }
    catch (Exception ex)
    {
      return JsonSerializer.Serialize(new { success = false, error = ex.Message });
    }
  }

  // Save chat history to storage
  [JSInvokable(nameof(SaveChatHistory))]
  public static async Task<string> SaveChatHistory(string key)
  {
    try
    {
      if (_chatService == null)
        throw new InvalidOperationException("Chat service not initialized. Call InitializeChat first.");

      await _chatService.SaveChatHistoryAsync(key);
      return JsonSerializer.Serialize(new { success = true, message = "Chat history saved" });
    }
    catch (Exception ex)
    {
      return JsonSerializer.Serialize(new { success = false, error = ex.Message });
    }
  }

  // Load chat history from storage
  [JSInvokable(nameof(LoadChatHistory))]
  public static async Task<string> LoadChatHistory(string key)
  {
    try
    {
      if (_chatService == null)
        throw new InvalidOperationException("Chat service not initialized. Call InitializeChat first.");

      await _chatService.LoadChatHistoryAsync(key);
      return JsonSerializer.Serialize(new { success = true, message = "Chat history loaded" });
    }
    catch (Exception ex)
    {
      return JsonSerializer.Serialize(new { success = false, error = ex.Message });
    }
  }
}

// Callback class for streaming responses
public class StreamingCallback
{
  [JSInvokable]
  public void OnChunk(string chunk)
  {
    // This will be called from C# to notify JavaScript of new chunks
  }

  [JSInvokable] 
  public void OnComplete()
  {
    // Called when streaming is complete
  }

  [JSInvokable]
  public void OnError(string error)
  {
    // Called when an error occurs during streaming
  }
}
