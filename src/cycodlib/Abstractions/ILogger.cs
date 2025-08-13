namespace Cycodlib.Abstractions
{
    /// <summary>
    /// Provides logging functionality abstraction for platform-agnostic logging
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Writes a debug message to the log
        /// </summary>
        /// <param name="message">The debug message to write</param>
        void WriteDebug(string message);

        /// <summary>
        /// Writes a warning message to the log
        /// </summary>
        /// <param name="message">The warning message to write</param>
        void WriteWarning(string message);

        /// <summary>
        /// Writes a normal message to the log
        /// </summary>
        /// <param name="message">The message to write</param>
        /// <param name="overrideQuiet">Whether to write the message even in quiet mode</param>
        void WriteLine(string message, bool overrideQuiet = false);

        /// <summary>
        /// Writes an error message to the log
        /// </summary>
        /// <param name="message">The error message to write</param>
        void WriteError(string message);

        /// <summary>
        /// Gets a value indicating whether debug logging is enabled
        /// </summary>
        bool IsDebugEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether the logger is in quiet mode
        /// </summary>
        bool IsQuietMode { get; }
    }
}