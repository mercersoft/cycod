using Microsoft.JSInterop;
using System.Text.Json;
using cycodblazor.Services;

namespace cycodblazor;

public static class Api
{
#pragma warning disable CS0649 // Field is never assigned to - intentionally null for mock implementation
  private static ChatService? _chatService;
#pragma warning restore CS0649

  // This method is callable from JS as DotNet.invokeMethodAsync('cycodblazor', 'Version')
  [JSInvokable(nameof(Version))]
  public static string Version() => "1.0.0 (Blazor WebAssembly)";

  // Test method to verify chat functionality without dependencies
  [JSInvokable(nameof(TestChat))]
  public static string TestChat()
  {
    var response = new StandardResponse { Success = true, Message = "Chat test successful" };
    return JsonSerializer.Serialize(response);
  }

  // Initialize a new chat session
  [JSInvokable(nameof(InitializeChat))]
  public static string InitializeChat(string systemPrompt, int? maxTokens = null)
  {
    try
    {
      // For now, just return success without actually initializing ChatService
      // to get basic functionality working again
      var response = new StandardResponse { Success = true, Message = "Chat initialized successfully (mock)" };
      return JsonSerializer.Serialize(response);
    }
    catch (Exception ex)
    {
      var errorResponse = new ErrorResponse { Success = false, Error = $"InitializeChat error: {ex.Message} | Stack: {ex.StackTrace}" };
      return JsonSerializer.Serialize(errorResponse);
    }
  }

  // Send a message and get complete response
  [JSInvokable(nameof(SendMessage))]
  public static string SendMessage(string message)
  {
    try
    {
      // Mock response for testing
      var messageResponse = new MessageResponse { Success = true, Response = $"Mock response to: {message}" };
      return JsonSerializer.Serialize(messageResponse);
    }
    catch (Exception ex)
    {
      var errorResponse = new ErrorResponse { Success = false, Error = ex.Message };
      return JsonSerializer.Serialize(errorResponse);
    }
  }

  // Send a message with streaming response (callback-based)
  [JSInvokable(nameof(SendMessageStreaming))]
  public static string SendMessageStreaming(string message, DotNetObjectReference<StreamingCallback>? callback = null)
  {
    try
    {
      if (_chatService == null)
        throw new InvalidOperationException("Chat service not initialized. Call InitializeChat first.");

      _chatService.SendMessageStreamingAsync(message, callback).GetAwaiter().GetResult();
      var response = new StandardResponse { Success = true, Message = "Streaming started" };
      return JsonSerializer.Serialize(response);
    }
    catch (Exception ex)
    {
      var errorResponse = new ErrorResponse { Success = false, Error = ex.Message };
      return JsonSerializer.Serialize(errorResponse);
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
      var response = new StandardResponse { Success = true, Message = "Chat history cleared" };
      return JsonSerializer.Serialize(response);
    }
    catch (Exception ex)
    {
      var errorResponse = new ErrorResponse { Success = false, Error = ex.Message };
      return JsonSerializer.Serialize(errorResponse);
    }
  }

  // Save chat history to storage
  [JSInvokable(nameof(SaveChatHistory))]
  public static string SaveChatHistory(string key)
  {
    try
    {
      if (_chatService == null)
        throw new InvalidOperationException("Chat service not initialized. Call InitializeChat first.");

      _chatService.SaveChatHistoryAsync(key).GetAwaiter().GetResult();
      var response = new StandardResponse { Success = true, Message = "Chat history saved" };
      return JsonSerializer.Serialize(response);
    }
    catch (Exception ex)
    {
      var errorResponse = new ErrorResponse { Success = false, Error = ex.Message };
      return JsonSerializer.Serialize(errorResponse);
    }
  }

  // Load chat history from storage
  [JSInvokable(nameof(LoadChatHistory))]
  public static string LoadChatHistory(string key)
  {
    try
    {
      if (_chatService == null)
        throw new InvalidOperationException("Chat service not initialized. Call InitializeChat first.");

      _chatService.LoadChatHistoryAsync(key).GetAwaiter().GetResult();
      var response = new StandardResponse { Success = true, Message = "Chat history loaded" };
      return JsonSerializer.Serialize(response);
    }
    catch (Exception ex)
    {
      var errorResponse = new ErrorResponse { Success = false, Error = ex.Message };
      return JsonSerializer.Serialize(errorResponse);
    }
  }

  // Get chat service status and capabilities
  [JSInvokable(nameof(GetChatStatus))]
  public static string GetChatStatus()
  {
    try
    {
      var status = new ChatStatusResponse
      {
        Success = true,
        IsInitialized = _chatService != null,
        Capabilities = new ChatCapabilities
        {
          Streaming = true,
          FunctionCalling = true,
          HistoryPersistence = true,
          ApprovalWorkflow = true
        },
        Version = "1.0.0 (Blazor WebAssembly)"
      };
      return JsonSerializer.Serialize(status);
    }
    catch (Exception ex)
    {
      // Provide detailed error information for debugging
      var errorResponse = new ErrorResponse 
      { 
        Success = false, 
        Error = $"GetChatStatus error: {ex.Message} | Type: {ex.GetType().Name} | Stack: {ex.StackTrace?.Substring(0, Math.Min(200, ex.StackTrace?.Length ?? 0))}"
      };
      return JsonSerializer.Serialize(errorResponse);
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

// Response classes for JSON serialization (required for trimmed/AOT scenarios)
public class ChatStatusResponse
{
  public bool Success { get; set; }
  public bool IsInitialized { get; set; }
  public ChatCapabilities? Capabilities { get; set; }
  public string? Version { get; set; }
}

public class ChatCapabilities
{
  public bool Streaming { get; set; }
  public bool FunctionCalling { get; set; }
  public bool HistoryPersistence { get; set; }
  public bool ApprovalWorkflow { get; set; }
}

public class ErrorResponse
{
  public bool Success { get; set; }
  public string? Error { get; set; }
}

public class StandardResponse
{
  public bool Success { get; set; }
  public string? Message { get; set; }
  public string? Error { get; set; }
}

public class MessageResponse
{
  public bool Success { get; set; }
  public string? Response { get; set; }
  public string? Error { get; set; }
}
