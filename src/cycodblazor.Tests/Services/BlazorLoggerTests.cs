using Microsoft.JSInterop;
using Moq;
using Xunit;
using Cycodblazor.Services;

namespace Cycodblazor.Tests.Services
{
    public class BlazorLoggerTests
    {
        private readonly Mock<IJSRuntime> _mockJSRuntime;
        private readonly BlazorLogger _logger;

        public BlazorLoggerTests()
        {
            _mockJSRuntime = new Mock<IJSRuntime>();
            _logger = new BlazorLogger(_mockJSRuntime.Object);
        }

        [Fact]
        public void WriteDebug_AddsLogEntry_WhenDebugEnabled()
        {
            // Arrange
            var logEntries = new List<LogEntry>();
            _logger.LogEntryAdded += logEntries.Add;

            // Act
            _logger.WriteDebug("Test debug message");

            // Assert
            Assert.Single(logEntries);
            Assert.Equal(LogLevel.Debug, logEntries[0].Level);
            Assert.Equal("Test debug message", logEntries[0].Message);
        }

        [Fact]
        public void WriteError_CallsConsoleError_AndAddsLogEntry()
        {
            // Arrange
            var logEntries = new List<LogEntry>();
            _logger.LogEntryAdded += logEntries.Add;

            // Act
            _logger.WriteError("Test error");

            // Assert
            _mockJSRuntime.Verify(js => js.InvokeAsync<IJSVoidResult>(
                "console.error", 
                It.Is<object[]>(args => args[0].ToString().Contains("Test error"))
            ), Times.Once);
            
            Assert.Single(logEntries);
            Assert.Equal(LogLevel.Error, logEntries[0].Level);
        }

        [Fact]
        public void GetRecentLogEntries_ReturnsCorrectCount()
        {
            // Arrange
            for (int i = 0; i < 10; i++)
            {
                _logger.WriteLine($"Message {i}");
            }

            // Act
            var recent = _logger.GetRecentLogEntries(5);

            // Assert
            Assert.Equal(5, recent.Count());
        }

        [Fact]
        public void ClearLogs_RemovesAllEntries()
        {
            // Arrange
            _logger.WriteLine("Test message");
            
            // Act
            _logger.ClearLogs();
            var entries = _logger.GetRecentLogEntries();

            // Assert
            Assert.Empty(entries);
        }
    }
}