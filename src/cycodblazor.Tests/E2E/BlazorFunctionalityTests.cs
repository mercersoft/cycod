using Microsoft.Playwright;
using Xunit;

namespace Cycodblazor.Tests.E2E
{
    public class BlazorFunctionalityTests : IAsyncLifetime
    {
        private IPlaywright _playwright = null!;
        private IBrowser _browser = null!;
        private IBrowserContext _context = null!;
        private IPage _page = null!;

        public async Task InitializeAsync()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
            _context = await _browser.NewContextAsync();
            _page = await _context.NewPageAsync();
        }

        public async Task DisposeAsync()
        {
            await _page?.CloseAsync();
            await _context?.CloseAsync();
            await _browser?.CloseAsync();
            _playwright?.Dispose();
        }

        [Fact]
        public async Task BrowserStorage_CanStoreAndRetrieveData()
        {
            // Navigate to test page
            await _page.GotoAsync("http://localhost:5000/test");

            // Test localStorage functionality
            await _page.EvaluateAsync(@"
                window.testStorage = {
                    storageProvider: new BrowserStorageProvider(DotNet)
                };
            ");

            // Store data
            await _page.EvaluateAsync(@"
                await window.testStorage.storageProvider.WriteTextAsync('test-key', 'test-value');
            ");

            // Retrieve data
            var result = await _page.EvaluateAsync<string>(@"
                return await window.testStorage.storageProvider.ReadTextAsync('test-key');
            ");

            Assert.Equal("test-value", result);
        }

        [Fact]
        public async Task IndexedDB_HandlesLargeData()
        {
            await _page.GotoAsync("http://localhost:5000/test");

            // Create large data (>1MB)
            var largeData = new string('x', 1024 * 1024 + 100);

            await _page.EvaluateAsync($@"
                window.testData = '{largeData}';
                window.testStorage = {{
                    storageProvider: new BrowserStorageProvider(DotNet, 1024 * 1024)
                }};
            ");

            // Store large data (should use IndexedDB)
            await _page.EvaluateAsync(@"
                await window.testStorage.storageProvider.WriteTextAsync('large-key', window.testData);
            ");

            // Verify data was stored in IndexedDB, not localStorage
            var localStorageValue = await _page.EvaluateAsync<string>(@"
                return localStorage.getItem('large-key');
            ");
            Assert.Null(localStorageValue); // Should not be in localStorage

            // Retrieve from IndexedDB
            var retrievedData = await _page.EvaluateAsync<string>(@"
                return await window.testStorage.storageProvider.ReadTextAsync('large-key');
            ");

            Assert.Equal(largeData, retrievedData);
        }

        [Fact]
        public async Task Logger_WritesToBrowserConsole()
        {
            await _page.GotoAsync("http://localhost:5000/test");

            var consoleMessages = new List<string>();
            _page.Console += (_, msg) => consoleMessages.Add(msg.Text);

            // Test logger functionality
            await _page.EvaluateAsync(@"
                const logger = new BlazorLogger(DotNet);
                logger.WriteLine('Test info message');
                logger.WriteError('Test error message');
                logger.WriteDebug('Test debug message');
            ");

            // Wait for console messages
            await _page.WaitForTimeoutAsync(100);

            Assert.Contains(consoleMessages, msg => msg.Contains("Test info message"));
            Assert.Contains(consoleMessages, msg => msg.Contains("Test error message"));
            Assert.Contains(consoleMessages, msg => msg.Contains("Test debug message"));
        }

        [Fact]
        public async Task LogViewer_DisplaysAndUpdatesLogs()
        {
            await _page.GotoAsync("http://localhost:5000/log-viewer-test");

            // Verify LogViewer component is rendered
            await _page.WaitForSelectorAsync(".log-viewer");

            // Initially should show "No log entries"
            var emptyMessage = await _page.TextContentAsync(".log-empty");
            Assert.Equal("No log entries", emptyMessage);

            // Add a log entry
            await _page.EvaluateAsync(@"
                window.testLogger.WriteLine('Dynamic test message');
            ");

            // Verify log appears
            await _page.WaitForSelectorAsync(".log-entry");
            var logEntry = await _page.TextContentAsync(".log-entry .log-message");
            Assert.Contains("Dynamic test message", logEntry);

            // Test clear functionality
            await _page.ClickAsync("button:has-text('Clear')");
            
            // Should show empty message again
            await _page.WaitForSelectorAsync(".log-empty");
            var clearedMessage = await _page.TextContentAsync(".log-empty");
            Assert.Equal("No log entries", clearedMessage);
        }

        [Fact]
        public async Task FunctionCallingChat_WorksWithBrowserStorage()
        {
            await _page.GotoAsync("http://localhost:5000/chat-test");

            // Setup chat with browser storage
            await _page.EvaluateAsync(@"
                const logger = new BlazorLogger(DotNet);
                const storage = new BrowserStorageProvider(DotNet);
                const functionFactory = new FunctionFactory(logger);
                
                window.testChat = new FunctionCallingChat(
                    null, // Mock chat client
                    'Test system prompt',
                    functionFactory,
                    logger,
                    null,
                    null,
                    storage
                );
            ");

            // Test saving chat history
            await _page.EvaluateAsync(@"
                const chatHistory = [
                    { role: 'user', content: 'Hello' },
                    { role: 'assistant', content: 'Hi there!' }
                ];
                await window.testChat.SaveChatHistoryAsync('test-chat', chatHistory);
            ");

            // Test loading chat history
            var loadedHistory = await _page.EvaluateAsync<object>(@"
                return await window.testChat.LoadChatHistoryAsync('test-chat');
            ");

            Assert.NotNull(loadedHistory);
        }

        [Fact]
        public async Task BrowserConfiguration_AccessesSettings()
        {
            await _page.GotoAsync("http://localhost:5000/config-test");

            // Test configuration provider
            await _page.EvaluateAsync(@"
                window.configProvider = new BlazorConfigurationProvider();
            ");

            // Test setting and getting values
            await _page.EvaluateAsync(@"
                await window.configProvider.SaveConfigValueAsync('test-setting', 'test-value');
            ");

            var retrievedValue = await _page.EvaluateAsync<string>(@"
                return window.configProvider.GetConfigValue('test-setting');
            ");

            Assert.Equal("test-value", retrievedValue);

            // Test environment variable access
            var userAgent = await _page.EvaluateAsync<string>(@"
                return window.configProvider.GetEnvironmentVariable('HTTP_USER_AGENT') || 
                       window.configProvider.GetEnvironmentVariable('USER_AGENT') ||
                       navigator.userAgent;
            ");

            Assert.NotNull(userAgent);
        }
    }
}