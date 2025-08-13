using Cycodlib.Abstractions;

namespace Cycod.Implementation
{
    /// <summary>
    /// Enhanced console implementation of ILogger with better formatting and error handling
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        private readonly string _prefix;
        private readonly bool _useColors;

        public ConsoleLogger(string prefix = "", bool useColors = true)
        {
            _prefix = prefix;
            _useColors = useColors;
        }

        public void WriteDebug(string message)
        {
            if (IsDebugEnabled)
            {
                var formattedMessage = FormatMessage("DEBUG", message);
                if (_useColors)
                {
                    ConsoleHelpers.WriteLine(formattedMessage, ConsoleColor.Gray);
                }
                else
                {
                    ConsoleHelpers.WriteDebugLine(formattedMessage);
                }
            }
        }

        public void WriteWarning(string message)
        {
            try
            {
                var formattedMessage = FormatMessage("WARNING", message);
                if (_useColors)
                {
                    ConsoleHelpers.WriteLine(formattedMessage, ConsoleColor.Yellow);
                }
                else
                {
                    ConsoleHelpers.WriteWarning(formattedMessage);
                }
            }
            catch (Exception ex)
            {
                // Fallback to basic output if formatting fails
                ConsoleHelpers.WriteWarning($"[LOG ERROR] {ex.Message} | Original: {message}");
            }
        }

        public void WriteLine(string message, bool overrideQuiet = false)
        {
            try
            {
                var formattedMessage = FormatMessage("INFO", message);
                ConsoleHelpers.WriteLine(formattedMessage, null, overrideQuiet);
            }
            catch (Exception ex)
            {
                // Fallback to basic output if formatting fails
                ConsoleHelpers.WriteLine($"[LOG ERROR] {ex.Message} | Original: {message}", null, overrideQuiet);
            }
        }

        public void WriteError(string message)
        {
            try
            {
                var formattedMessage = FormatMessage("ERROR", message);
                if (_useColors)
                {
                    ConsoleHelpers.WriteLine(formattedMessage, ConsoleColor.Red);
                }
                else
                {
                    ConsoleHelpers.WriteError(formattedMessage);
                }
            }
            catch (Exception ex)
            {
                // Fallback to basic output if formatting fails
                ConsoleHelpers.WriteError($"[LOG ERROR] {ex.Message} | Original: {message}");
            }
        }

        public bool IsDebugEnabled => ConsoleHelpers.IsDebug();

        public bool IsQuietMode => ConsoleHelpers.IsQuiet();

        private string FormatMessage(string level, string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var prefix = string.IsNullOrEmpty(_prefix) ? "" : $"[{_prefix}] ";
            return $"[{timestamp}] {prefix}[{level}] {message}";
        }

        /// <summary>
        /// Creates a logger with a specific prefix for component identification
        /// </summary>
        public static ConsoleLogger ForComponent(string componentName, bool useColors = true)
        {
            return new ConsoleLogger(componentName, useColors);
        }
    }
}