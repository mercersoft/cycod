using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using Xunit;
using Cycodblazor.Components;
using Cycodblazor.Services;

namespace Cycodblazor.Tests.Components
{
    public class LogViewerIntegrationTests : TestContext
    {
        private Mock<IJSRuntime> _mockJSRuntime;
        private BlazorLogger _logger;

        public LogViewerIntegrationTests()
        {
            _mockJSRuntime = new Mock<IJSRuntime>();
            _logger = new BlazorLogger(_mockJSRuntime.Object);
            
            Services.AddSingleton(_logger);
            Services.AddSingleton(_mockJSRuntime.Object);
        }

        [Fact]
        public void LogViewer_DisplaysLogEntries()
        {
            // Arrange
            _logger.WriteLine("Test message 1");
            _logger.WriteError("Test error");
            _logger.WriteDebug("Debug message");

            // Act
            var component = RenderComponent<LogViewer>();

            // Assert
            var logEntries = component.FindAll(".log-entry");
            Assert.Equal(3, logEntries.Count);
            
            Assert.Contains("Test message 1", component.Markup);
            Assert.Contains("Test error", component.Markup);
            Assert.Contains("Debug message", component.Markup);
        }

        [Fact]
        public void LogViewer_FiltersDebugMessages_WhenShowDebugFalse()
        {
            // Arrange
            _logger.WriteLine("Info message");
            _logger.WriteDebug("Debug message");
            _logger.WriteError("Error message");

            var component = RenderComponent<LogViewer>();

            // Act - Uncheck show debug
            var showDebugCheckbox = component.Find("input[type='checkbox']");
            showDebugCheckbox.Change(false);

            // Assert
            Assert.Contains("Info message", component.Markup);
            Assert.Contains("Error message", component.Markup);
            Assert.DoesNotContain("Debug message", component.Markup);
        }

        [Fact]
        public void LogViewer_ClearsLogs_WhenClearButtonClicked()
        {
            // Arrange
            _logger.WriteLine("Test message");
            var component = RenderComponent<LogViewer>();
            
            // Verify message exists
            Assert.Contains("Test message", component.Markup);

            // Act
            var clearButton = component.Find("button");
            clearButton.Click();

            // Assert
            Assert.DoesNotContain("Test message", component.Markup);
            Assert.Contains("No log entries", component.Markup);
        }

        [Fact]
        public void LogViewer_UpdatesInRealTime_WhenNewLogAdded()
        {
            // Arrange
            var component = RenderComponent<LogViewer>();
            
            // Initially no logs
            Assert.Contains("No log entries", component.Markup);

            // Act - Add log after component is rendered
            _logger.WriteLine("New log message");

            // Assert - Component should update automatically
            Assert.Contains("New log message", component.Markup);
            Assert.DoesNotContain("No log entries", component.Markup);
        }

        [Fact]
        public void LogViewer_LimitsLogEntries_ToMaximum()
        {
            // Arrange
            var component = RenderComponent<LogViewer>();

            // Act - Add more than 1000 log entries
            for (int i = 0; i < 1100; i++)
            {
                _logger.WriteLine($"Message {i}");
            }

            // Assert - Should only show recent entries (not more than 1000)
            var logEntries = component.FindAll(".log-entry");
            Assert.True(logEntries.Count <= 1000);
            
            // Should contain recent messages
            Assert.Contains("Message 1099", component.Markup);
            // Should not contain very old messages
            Assert.DoesNotContain("Message 0", component.Markup);
        }
    }
}