using Cycodlib.Core;
using Cycodlib.Core.Chat;
using Cycodlib.Core.Formatting;
using Cycodlib.Functions;
using Cycodlib.Chat;
using Cycodlib.Abstractions;
using Cycodblazor.Services;
using Microsoft.JSInterop;
using Microsoft.Extensions.AI;

namespace Cycodblazor
{
    /// <summary>
    /// Example of how to use the components from cycodlib in Blazor
    /// </summary>
    public class ExampleUsage
    {
        private readonly IJSRuntime _jsRuntime;

        public ExampleUsage(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task DemonstrateUsageAsync()
        {
            // Phase 2 components (Pure logic - works as-is)
            // Using CycoDevProgramInfo
            var programInfo = new CycoDevProgramInfo();
            
            // Using ChatHistoryDefaults
            bool useOpenAIFormat = ChatHistoryDefaults.UseOpenAIFormat;
            
            // Using TrajectoryFormatter
            string formattedUser = TrajectoryFormatter.FormatUserInput("Hello, AI!");
            string formattedAssistant = TrajectoryFormatter.FormatAssistantOutput("Hello! How can I help?");
            
            // Using DateAndTimeHelperFunctions
            var dateTimeHelper = new DateAndTimeHelperFunctions();
            string currentDate = dateTimeHelper.GetCurrentDate();
            string currentTime = dateTimeHelper.GetCurrentTime();

            // Phase 3 components (Refactored with abstractions)
            // Create Blazor-specific service implementations
            var logger = new BlazorLogger(_jsRuntime);
            var storageProvider = new BrowserStorageProvider(_jsRuntime);
            
            // Using FunctionFactory with dependency injection
            var functionFactory = new FunctionFactory(logger);
            functionFactory.AddFunctions(dateTimeHelper);
            
            // Using FunctionCallDetector with dependency injection
            var functionCallDetector = new FunctionCallDetector(logger);
            
            // Using FunctionCallingChat with dependency injection
            // Note: You would need to provide an actual IChatClient implementation
            // var chatClient = CreateChatClient(); // Implementation depends on your AI service
            // var functionCallingChat = new FunctionCallingChat(
            //     chatClient, 
            //     "You are a helpful AI assistant.", 
            //     functionFactory, 
            //     logger,
            //     storageProvider: storageProvider);
            
            // Example of async operations
            await storageProvider.WriteTextAsync("example-key", "Hello Blazor!");
            var savedContent = await storageProvider.ReadTextAsync("example-key");
            
            logger.WriteLine($"Saved and retrieved: {savedContent}");
        }
    }
}