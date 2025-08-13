using Microsoft.JSInterop;
using Cycodlib.Abstractions;
using System.Collections.Concurrent;

namespace Cycodblazor.Services
{
    /// <summary>
    /// Enhanced Blazor implementation of ILogger with UI logging capabilities
    /// </summary>
    public class BlazorLogger : Cycodlib.Abstractions.ILogger
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly bool _isDebugEnabled;
        private readonly bool _isQuietMode;
        private readonly ConcurrentQueue<LogEntry> _logQueue = new();
        private readonly int _maxLogEntries;

        public BlazorLogger(IJSRuntime jsRuntime, bool isDebugEnabled = true, bool isQuietMode = false, int maxLogEntries = 1000)
        {
            _jsRuntime = jsRuntime;
            _isDebugEnabled = isDebugEnabled;
            _isQuietMode = isQuietMode;
            _maxLogEntries = maxLogEntries;
        }

        public event Action<LogEntry>? LogEntryAdded;

        public void WriteDebug(string message)
        {
            if (_isDebugEnabled)
            {
                AddLogEntry(LogLevel.Debug, message);
                _ = WriteToConsoleAsync("console.log", $"[DEBUG] {message}");
            }
        }

        public void WriteWarning(string message)
        {
            AddLogEntry(LogLevel.Warning, message);
            _ = WriteToConsoleAsync("console.warn", $"[WARNING] {message}");
        }

        public void WriteLine(string message, bool overrideQuiet = false)
        {
            if (!_isQuietMode || overrideQuiet)
            {
                AddLogEntry(LogLevel.Information, message);
                _ = WriteToConsoleAsync("console.log", message);
            }
        }

        public void WriteError(string message)
        {
            AddLogEntry(LogLevel.Error, message);
            _ = WriteToConsoleAsync("console.error", $"[ERROR] {message}");
        }

        public bool IsDebugEnabled => _isDebugEnabled;

        public bool IsQuietMode => _isQuietMode;

        /// <summary>
        /// Gets the recent log entries for UI display
        /// </summary>
        public IEnumerable<LogEntry> GetRecentLogEntries(int count = 100)
        {
            return _logQueue.TakeLast(Math.Min(count, _logQueue.Count));
        }

        /// <summary>
        /// Clears all log entries
        /// </summary>
        public void ClearLogs()
        {
            while (_logQueue.TryDequeue(out _)) { }
        }

        private void AddLogEntry(LogLevel level, string message)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message
            };

            _logQueue.Enqueue(entry);

            // Maintain max log entries limit
            while (_logQueue.Count > _maxLogEntries)
            {
                _logQueue.TryDequeue(out _);
            }

            // Notify subscribers
            LogEntryAdded?.Invoke(entry);
        }

        private async Task WriteToConsoleAsync(string consoleMethod, string message)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync(consoleMethod, message);
            }
            catch
            {
                // Ignore JS interop errors - browser console might not be available
            }
        }
    }

    /// <summary>
    /// Represents a log entry for UI display
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;

        public string FormattedMessage => $"[{Timestamp:HH:mm:ss.fff}] [{Level}] {Message}";

        public string CssClass => Level switch
        {
            LogLevel.Debug => "log-debug",
            LogLevel.Information => "log-info",
            LogLevel.Warning => "log-warning",
            LogLevel.Error => "log-error",
            _ => "log-info"
        };
    }

    /// <summary>
    /// Log levels for categorizing log messages
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Information,
        Warning,
        Error
    }
}