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

  // Get chat service status and capabilities
  [JSInvokable(nameof(GetChatStatus))]
  public static string GetChatStatus()
  {
    try
    {
      var status = new
      {
        success = true,
        isInitialized = _chatService != null,
        capabilities = new
        {
          streaming = true,
          functionCalling = true,
          historyPersistence = true,
          approvalWorkflow = true
        },
        version = "1.0.0"
      };
      return JsonSerializer.Serialize(status);
    }
    catch (Exception ex)
    {
      return JsonSerializer.Serialize(new { success = false, error = ex.Message });
    }
  }
}

// Callback class for streaming responses and function calling
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

  [JSInvokable]
  public async Task<bool> OnFunctionCallApproval(string functionName, string? functionArgs)
  {
    // Called when a function call requires approval
    try
    {
      var jsRuntime = GetJSRuntime();
      if (jsRuntime != null)
      {
        return await jsRuntime.InvokeAsync<bool>("chatHelpers.showFunctionCallApproval", functionName, functionArgs ?? "{}");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error showing function approval dialog: {ex.Message}");
    }
    
    // Default to deny if we can't show the approval dialog
    return false;
  }

  [JSInvokable]
  public void OnFunctionCall(string functionName, string? functionArgs, string? functionResult)
  {
    // Called when a function is executed
    Console.WriteLine($"Function called: {functionName} with args: {functionArgs} -> result: {functionResult}");
  }

  // Helper method to get JSRuntime (this would need to be injected properly in a real implementation)
  private static IJSRuntime? GetJSRuntime()
  {
    // TODO: This should be properly injected via DI
    // For now, this is a placeholder that would need to be implemented
    return null;
  }
}
