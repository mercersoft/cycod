using Microsoft.Extensions.AI;
using System.Text.Json;
using Cycodlib.Abstractions;
using Cycodlib.Core.Chat;
using Cycodlib.Functions;

namespace Cycodlib.Chat
{
    /// <summary>
    /// Manages AI chat interactions with function calling capabilities using dependency injection
    /// </summary>
    public class FunctionCallingChat : IAsyncDisposable
    {
        private readonly IChatClient _chatClient;
        private readonly string _systemPrompt;
        private readonly FunctionFactory _functionFactory;
        private readonly FunctionCallDetector _functionCallDetector;
        private readonly ChatOptions _options;
        private readonly ILogger _logger;
        private readonly IStorageProvider? _storageProvider;
        private readonly List<ChatMessage> _messages;
        private readonly List<ChatMessage> _userMessageAdds = new();

        public FunctionCallingChat(
            IChatClient chatClient, 
            string systemPrompt, 
            FunctionFactory functionFactory, 
            ILogger logger,
            ChatOptions? options = null, 
            int? maxOutputTokens = null,
            IStorageProvider? storageProvider = null)
        {
            _systemPrompt = systemPrompt;
            _functionFactory = functionFactory ?? throw new ArgumentNullException(nameof(functionFactory));
            _functionCallDetector = new FunctionCallDetector(logger);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storageProvider = storageProvider;

            var useMicrosoftExtensionsAIFunctionCalling = false; // Can't use this for now; (1) doesn't work with copilot w/ all models, (2) functionCallCallback not available
            _chatClient = useMicrosoftExtensionsAIFunctionCalling
                ? chatClient.AsBuilder().UseFunctionInvocation().Build()
                : chatClient;

            var tools = _functionFactory.GetAITools().ToList();
            _logger.WriteDebug($"FunctionCallingChat: Found {tools.Count} tools in FunctionFactory");

            _messages = new List<ChatMessage>();
            _options = new ChatOptions()
            {
                ModelId = options?.ModelId,
                ToolMode = options?.ToolMode,
                Tools = tools,
                MaxOutputTokens = maxOutputTokens.HasValue
                    ? maxOutputTokens.Value
                    : options?.MaxOutputTokens,
            };

            if (maxOutputTokens.HasValue) _options.MaxOutputTokens = maxOutputTokens.Value;

            ClearChatHistory();
        }

        public void ClearChatHistory()
        {
            _messages.Clear();
            _messages.Add(new ChatMessage(ChatRole.System, _systemPrompt));
            _messages.AddRange(_userMessageAdds);
        }

        public void AddUserMessage(string userMessage, int maxPromptTokenTarget = 0, int maxChatTokenTarget = 0)
        {
            _userMessageAdds.Add(new ChatMessage(ChatRole.User, userMessage));
            _userMessageAdds.TryTrimToTarget(
                maxPromptTokenTarget: maxPromptTokenTarget,
                maxChatTokenTarget: maxChatTokenTarget);

            _messages.Add(new ChatMessage(ChatRole.User, userMessage));
            _messages.TryTrimToTarget(
                maxPromptTokenTarget: maxPromptTokenTarget,
                maxChatTokenTarget: maxChatTokenTarget);
        }
        
        public void AddUserMessages(IEnumerable<string> userMessages, int maxPromptTokenTarget = 0, int maxChatTokenTarget = 0)
        {
            foreach (var userMessage in userMessages)
            {
                AddUserMessage(userMessage, maxPromptTokenTarget);
            }

            _messages.TryTrimToTarget(maxChatTokenTarget: maxChatTokenTarget);
        }

        public async Task LoadChatHistoryAsync(string key, int maxPromptTokenTarget = 0, int maxToolTokenTarget = 0, int maxChatTokenTarget = 0, bool useOpenAIFormat = ChatHistoryDefaults.UseOpenAIFormat)
        {
            if (_storageProvider == null)
            {
                _logger.WriteWarning("No storage provider available for loading chat history");
                return;
            }

            try
            {
                var content = await _storageProvider.ReadTextAsync(key);
                if (content != null)
                {
                    var messages = DeserializeChatHistory(content, useOpenAIFormat);
                    _messages.Clear();
                    _messages.AddRange(messages);
                    _messages.FixDanglingToolCalls();
                    _messages.TryTrimToTarget(maxPromptTokenTarget, maxToolTokenTarget, maxChatTokenTarget);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteError($"Failed to load chat history from {key}: {ex.Message}");
            }
        }

        public async Task SaveChatHistoryAsync(string key, bool useOpenAIFormat = ChatHistoryDefaults.UseOpenAIFormat)
        {
            if (_storageProvider == null)
            {
                _logger.WriteWarning("No storage provider available for saving chat history");
                return;
            }

            try
            {
                var content = SerializeChatHistory(_messages, useOpenAIFormat);
                await _storageProvider.WriteTextAsync(key, content);
            }
            catch (Exception ex)
            {
                _logger.WriteError($"Failed to save chat history to {key}: {ex.Message}");
            }
        }

        public async Task SaveTrajectoryAsync(string key, bool useOpenAIFormat = ChatHistoryDefaults.UseOpenAIFormat)
        {
            if (_storageProvider == null)
            {
                _logger.WriteWarning("No storage provider available for saving trajectory");
                return;
            }

            try
            {
                var content = SerializeTrajectory(_messages, useOpenAIFormat);
                await _storageProvider.WriteTextAsync(key, content);
            }
            catch (Exception ex)
            {
                _logger.WriteError($"Failed to save trajectory to {key}: {ex.Message}");
            }
        }

        public async Task<string> CompleteChatStreamingAsync(
            string userPrompt,
            Action<IList<ChatMessage>>? messageCallback = null,
            Action<ChatResponseUpdate>? streamingCallback = null,
            Func<string, string?, bool>? approveFunctionCall = null,
            Action<string, string, string?>? functionCallCallback = null)
        {
            _messages.Add(new ChatMessage(ChatRole.User, userPrompt));
            messageCallback?.Invoke(_messages);

            var contentToReturn = string.Empty;
            while (true)
            {
                var responseContent = string.Empty;
                await foreach (var update in _chatClient.GetStreamingResponseAsync(_messages, _options))
                {
                    _functionCallDetector.CheckForFunctionCall(update);

                    var content = string.Join("", update.Contents
                        .Where(c => c is TextContent)
                        .Cast<TextContent>()
                        .Select(c => c.Text)
                        .ToList());

                    if (update.FinishReason == ChatFinishReason.ContentFilter)
                    {
                        content = $"{content}\nWARNING: Content filtered!";
                    }

                    responseContent += content;
                    contentToReturn += content;

                    streamingCallback?.Invoke(update);
                }

                if (TryCallFunctions(responseContent, approveFunctionCall, functionCallCallback, messageCallback))
                {
                    _functionCallDetector.Clear();
                    continue;
                }

                _messages.Add(new ChatMessage(ChatRole.Assistant, responseContent));
                messageCallback?.Invoke(_messages);

                return contentToReturn;
            }
        }

        private bool TryCallFunctions(string responseContent, Func<string, string?, bool>? approveFunctionCall, Action<string, string, string?>? functionCallCallback, Action<IList<ChatMessage>>? messageCallback)
        {
            var noFunctionsToCall = !_functionCallDetector.HasFunctionCalls();
            if (noFunctionsToCall) return false;
            
            var readyToCallFunctionCalls = _functionCallDetector.GetReadyToCallFunctionCalls();

            var assistentContent = readyToCallFunctionCalls.AsAIContentList();
            _messages.Add(new ChatMessage(ChatRole.Assistant, [.. assistentContent]));

            _logger.WriteDebug($"Calling functions: {string.Join(", ", readyToCallFunctionCalls.Select(call => call.Name))}");

            var functionResults = new List<FunctionResultContent>();

            foreach (var functionCall in readyToCallFunctionCalls)
            {
                var isReadOnly = _functionFactory.IsReadOnlyFunction(functionCall.Name);
                var requiresApproval = !isReadOnly.GetValueOrDefault(false);

                var approved = !requiresApproval || (approveFunctionCall?.Invoke(functionCall.Name, functionCall.Arguments) ?? false);
                if (approved)
                {
                    _logger.WriteDebug($"Calling function: {functionCall.Name} with arguments: {functionCall.Arguments}");
                    var success = _functionFactory.TryCallFunction(functionCall.Name, functionCall.Arguments, out var functionResult);
                    _logger.WriteDebug($"Function call result: {functionResult}");

                    functionCallCallback?.Invoke(functionCall.Name, functionCall.Arguments, functionResult);

                    functionResults.Add(new FunctionResultContent(functionCall.CallId, functionResult));
                }
                else
                {
                    _logger.WriteDebug($"Function call not approved: {functionCall.Name} with arguments: {functionCall.Arguments}");
                    functionResults.Add(new FunctionResultContent(functionCall.CallId, "Function call not approved"));
                }
            }

            _messages.Add(new ChatMessage(ChatRole.Tool, functionResults.Cast<AIContent>().ToArray()));
            messageCallback?.Invoke(_messages);

            return true;
        }

        private static List<ChatMessage> DeserializeChatHistory(string content, bool useOpenAIFormat)
        {
            // Implementation would depend on the specific format
            // This is a placeholder - actual implementation would need the specific serialization logic
            return JsonSerializer.Deserialize<List<ChatMessage>>(content) ?? new List<ChatMessage>();
        }

        private static string SerializeChatHistory(List<ChatMessage> messages, bool useOpenAIFormat)
        {
            // Implementation would depend on the specific format
            // This is a placeholder - actual implementation would need the specific serialization logic
            return JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true });
        }

        private static string SerializeTrajectory(List<ChatMessage> messages, bool useOpenAIFormat)
        {
            // Implementation would depend on the specific trajectory format
            // This is a placeholder - actual implementation would need the specific serialization logic
            return JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true });
        }

        public async ValueTask DisposeAsync()
        {
            if (_chatClient is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (_chatClient is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}