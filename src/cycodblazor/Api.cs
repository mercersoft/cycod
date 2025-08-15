using Microsoft.JSInterop;
using System.Text.Json;
using cycodblazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace cycodblazor;

public static class Api
{
  private static ChatService? _chatService;
  private static IServiceProvider? _serviceProvider;

  public static void SetServiceProvider(IServiceProvider serviceProvider)
  {
    _serviceProvider = serviceProvider;
  }

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
      // Get ChatService from DI container, fallback to manual creation
      if (_serviceProvider != null)
      {
        _chatService = _serviceProvider.GetRequiredService<ChatService>();
      }
      else
      {
        // Fallback to manual creation when DI is not available
        _chatService = new ChatService();
      }
      
      // Initialize the chat asynchronously and wait for completion
      _chatService.InitializeChatAsync(systemPrompt, maxTokens).GetAwaiter().GetResult();
      
      var response = new StandardResponse { Success = true, Message = "Chat initialized successfully" };
      return JsonSerializer.Serialize(response);
    }
    catch (Exception ex)
    {
      var errorResponse = new ErrorResponse { Success = false, Error = ex.Message };
      return JsonSerializer.Serialize(errorResponse);
    }
  }

  // Send a message and get complete response
  [JSInvokable(nameof(SendMessage))]
  public static string SendMessage(string message)
  {
    try
    {
      if (_chatService == null)
        throw new InvalidOperationException("Chat service not initialized. Call InitializeChat first.");

      var response = _chatService.SendMessageAsync(message).GetAwaiter().GetResult();
      var messageResponse = new MessageResponse { Success = true, Response = response };
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

  // Set configuration value
  [JSInvokable(nameof(SetConfig))]
  public static async Task<string> SetConfig(string key, string value)
  {
    try
    {
      // Get storage provider from DI and store the config value
      if (_serviceProvider != null)
      {
        var storageProvider = _serviceProvider.GetRequiredService<Cycodlib.Abstractions.IStorageProvider>();
        await storageProvider.WriteTextAsync($"config/{key}", value);
      }
      
      var response = new StandardResponse { Success = true, Message = $"Configuration set: {key}" };
      return JsonSerializer.Serialize(response);
    }
    catch (Exception ex)
    {
      var errorResponse = new ErrorResponse { Success = false, Error = ex.Message };
      return JsonSerializer.Serialize(errorResponse);
    }
  }

  // Get configuration value
  [JSInvokable(nameof(GetConfig))]
  public static async Task<string> GetConfig(string key)
  {
    try
    {
      string? value = null;
      
      // Get storage provider from DI and read the config value
      if (_serviceProvider != null)
      {
        var storageProvider = _serviceProvider.GetRequiredService<Cycodlib.Abstractions.IStorageProvider>();
        value = await storageProvider.ReadTextAsync($"config/{key}");
      }
      
      if (value != null)
      {
        var response = new ConfigValueResponse { Success = true, Value = value };
        return JsonSerializer.Serialize(response);
      }
      else
      {
        var response = new ConfigValueResponse { Success = false, Error = $"Configuration not found: {key}" };
        return JsonSerializer.Serialize(response);
      }
    }
    catch (Exception ex)
    {
      var errorResponse = new ErrorResponse { Success = false, Error = ex.Message };
      return JsonSerializer.Serialize(errorResponse);
    }
  }

  // List all configuration
  [JSInvokable(nameof(ListConfig))]
  public static async Task<string> ListConfig()
  {
    try
    {
      var items = new List<ConfigItem>();
      
      // Get storage provider from DI and list all config values
      if (_serviceProvider != null)
      {
        var storageProvider = _serviceProvider.GetRequiredService<Cycodlib.Abstractions.IStorageProvider>();
        var configKeys = await storageProvider.ListFilesAsync("config/");
        
        foreach (var configPath in configKeys)
        {
          var key = configPath.Substring("config/".Length); // Remove config/ prefix
          var value = await storageProvider.ReadTextAsync(configPath);
          
          if (value != null)
          {
            items.Add(new ConfigItem { Key = key, Value = value });
          }
        }
      }
      
      var response = new ConfigListResponse { Success = true, Items = items };
      return JsonSerializer.Serialize(response);
    }
    catch (Exception ex)
    {
      var errorResponse = new ErrorResponse { Success = false, Error = ex.Message };
      return JsonSerializer.Serialize(errorResponse);
    }
  }

  // Clear configuration
  [JSInvokable(nameof(ClearConfig))]
  public static async Task<string> ClearConfig(string? key = null)
  {
    try
    {
      // Get storage provider from DI and clear config
      if (_serviceProvider != null)
      {
        var storageProvider = _serviceProvider.GetRequiredService<Cycodlib.Abstractions.IStorageProvider>();
        
        if (key != null)
        {
          // Clear specific key
          await storageProvider.DeleteAsync($"config/{key}");
        }
        else
        {
          // Clear all config - list all keys and delete them
          var configKeys = await storageProvider.ListFilesAsync("config/");
          
          foreach (var configPath in configKeys)
          {
            await storageProvider.DeleteAsync(configPath);
          }
        }
      }
      
      var message = key != null ? $"Configuration cleared: {key}" : "All configuration cleared";
      var response = new StandardResponse { Success = true, Message = message };
      return JsonSerializer.Serialize(response);
    }
    catch (Exception ex)
    {
      var errorResponse = new ErrorResponse { Success = false, Error = ex.Message };
      return JsonSerializer.Serialize(errorResponse);
    }
  }
}

// Additional response classes for config operations
public class ConfigValueResponse
{
  public bool Success { get; set; }
  public string? Value { get; set; }
  public string? Error { get; set; }
}

public class ConfigListResponse
{
  public bool Success { get; set; }
  public List<ConfigItem>? Items { get; set; }
  public string? Error { get; set; }
}

public class ConfigItem
{
  public string Key { get; set; } = "";
  public string Value { get; set; } = "";
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
