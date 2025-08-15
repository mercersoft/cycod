using Microsoft.Extensions.AI;
using Microsoft.JSInterop;
using Cycodlib.Abstractions;
using Cycodlib.Chat;
using Cycodlib.Functions;
using Cycodblazor.Services;

namespace cycodblazor.Services;

public class ChatService : IAsyncDisposable
{
    private FunctionCallingChat? _chat;
    private readonly Cycodlib.Abstractions.ILogger _logger;
    private readonly IStorageProvider _storageProvider;
    private readonly FunctionFactory _functionFactory;
    private IChatClient? _chatClient;

    public ChatService()
    {
        // Initialize dependencies - these will be properly injected later
        _logger = CreateLogger();
        _storageProvider = CreateStorageProvider();
        _functionFactory = new FunctionFactory(_logger);
        
        // Chat client will be configured during initialization
        _chatClient = null;
    }

    public async Task InitializeChatAsync(string systemPrompt, int? maxTokens = null)
    {
        if (_chat != null)
        {
            await _chat.DisposeAsync();
        }

        // Create chat client if not already configured
        _chatClient ??= CreateChatClient();

        _chat = new FunctionCallingChat(
            chatClient: _chatClient,
            systemPrompt: systemPrompt,
            functionFactory: _functionFactory,
            logger: _logger,
            maxOutputTokens: maxTokens,
            storageProvider: _storageProvider
        );
    }

    public async Task<string> SendMessageAsync(string message)
    {
        if (_chat == null)
            throw new InvalidOperationException("Chat not initialized. Call InitializeChatAsync first.");

        return await _chat.CompleteChatStreamingAsync(message);
    }

    public async Task SendMessageStreamingAsync(string message, DotNetObjectReference<StreamingCallback>? callback = null)
    {
        if (_chat == null)
            throw new InvalidOperationException("Chat not initialized. Call InitializeChatAsync first.");

        try
        {
            await _chat.CompleteChatStreamingAsync(
                userPrompt: message,
                streamingCallback: update =>
                {
                    if (callback != null && update.Contents?.Any() == true)
                    {
                        var content = string.Join("", update.Contents
                            .Where(c => c is TextContent)
                            .Cast<TextContent>()
                            .Select(c => c.Text));
                        
                        if (!string.IsNullOrEmpty(content))
                        {
                            _ = Task.Run(() =>
                            {
                                try
                                {
                                    callback.Value.OnChunk(content);
                                }
                                catch (Exception ex)
                                {
                                    _logger.WriteError($"Error invoking streaming callback: {ex.Message}");
                                }
                            });
                        }
                    }
                },
                approveFunctionCall: callback != null 
                    ? (functionName, functionArgs) =>
                    {
                        try
                        {
                            // Convert to synchronous call for now - ideally this would be async
                            var task = callback.Value.OnFunctionCallApproval(functionName, functionArgs);
                            return task.GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteError($"Error getting function approval: {ex.Message}");
                            return false; // Default to deny on error
                        }
                    }
                    : null,
                functionCallCallback: callback != null 
                    ? (functionName, functionArgs, functionResult) =>
                    {
                        try
                        {
                            callback.Value.OnFunctionCall(functionName, functionArgs, functionResult);
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteError($"Error invoking function call callback: {ex.Message}");
                        }
                    }
                    : null
            );

            // Notify completion
            if (callback != null)
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        callback.Value.OnComplete();
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteError($"Error invoking completion callback: {ex.Message}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            // Notify error
            if (callback != null)
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        callback.Value.OnError(ex.Message);
                    }
                    catch (Exception callbackEx)
                    {
                        _logger.WriteError($"Error invoking error callback: {callbackEx.Message}");
                    }
                });
            }
            throw;
        }
    }

    public void ClearChatHistory()
    {
        _chat?.ClearChatHistory();
    }

    public async Task SaveChatHistoryAsync(string key)
    {
        if (_chat == null)
            throw new InvalidOperationException("Chat not initialized. Call InitializeChatAsync first.");

        await _chat.SaveChatHistoryAsync(key);
    }

    public async Task LoadChatHistoryAsync(string key)
    {
        if (_chat == null)
            throw new InvalidOperationException("Chat not initialized. Call InitializeChatAsync first.");

        await _chat.LoadChatHistoryAsync(key);
    }

    private Cycodlib.Abstractions.ILogger CreateLogger()
    {
        // For now, create a simple console logger
        // This should be replaced with proper DI when available
        return new SimpleConsoleLogger();
    }

    private IStorageProvider CreateStorageProvider()
    {
        // For now, create a simple in-memory storage provider
        // This should be replaced with proper browser storage when JSRuntime is available
        return new InMemoryStorageProvider();
    }

    private IChatClient CreateChatClient()
    {
        // Use the lightweight Blazor-specific chat client factory
        // This will check environment variables and throw detailed error messages
        // without pulling in heavy dependencies that don't work in WebAssembly
        return BlazorChatClientFactory.CreateChatClient();
    }

    public async ValueTask DisposeAsync()
    {
        if (_chat != null)
        {
            await _chat.DisposeAsync();
            _chat = null;
        }

        if (_chatClient is IAsyncDisposable asyncDisposableClient)
        {
            await asyncDisposableClient.DisposeAsync();
        }
        else if (_chatClient is IDisposable disposableClient)
        {
            disposableClient.Dispose();
        }
    }
}

// Simple console logger implementation for initial setup
internal class SimpleConsoleLogger : Cycodlib.Abstractions.ILogger
{
    public bool IsDebugEnabled => true;
    public bool IsQuietMode => false;

    public void WriteDebug(string message)
    {
        Console.WriteLine($"[DEBUG] {message}");
    }

    public void WriteLine(string message, bool overrideQuiet = false)
    {
        Console.WriteLine(message);
    }

    public void WriteWarning(string message)
    {
        Console.WriteLine($"[WARNING] {message}");
    }

    public void WriteError(string message)
    {
        Console.WriteLine($"[ERROR] {message}");
    }
}

// Simple in-memory storage provider for initial setup
internal class InMemoryStorageProvider : IStorageProvider
{
    private readonly Dictionary<string, string> _storage = new();

    public Task<string?> ReadTextAsync(string path)
    {
        _storage.TryGetValue(path, out var content);
        return Task.FromResult(content);
    }

    public Task WriteTextAsync(string path, string content)
    {
        _storage[path] = content;
        return Task.CompletedTask;
    }

    public Task AppendTextAsync(string path, string content)
    {
        if (_storage.TryGetValue(path, out var existing))
        {
            _storage[path] = existing + content;
        }
        else
        {
            _storage[path] = content;
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string path)
    {
        return Task.FromResult(_storage.ContainsKey(path));
    }

    public Task<IEnumerable<string>> ListFilesAsync(string directory, string pattern = "*")
    {
        var files = _storage.Keys.Where(k => k.StartsWith(directory));
        return Task.FromResult(files);
    }

    public Task DeleteAsync(string path)
    {
        _storage.Remove(path);
        return Task.CompletedTask;
    }

    public Task CreateDirectoryAsync(string path)
    {
        return Task.CompletedTask;
    }

    public Task<StorageItemInfo?> GetItemInfoAsync(string path)
    {
        if (_storage.TryGetValue(path, out var content))
        {
            return Task.FromResult<StorageItemInfo?>(new StorageItemInfo
            {
                FullPath = path,
                Name = System.IO.Path.GetFileName(path),
                IsDirectory = false,
                Size = System.Text.Encoding.UTF8.GetByteCount(content),
                LastModified = DateTime.Now
            });
        }
        return Task.FromResult<StorageItemInfo?>(null);
    }
}