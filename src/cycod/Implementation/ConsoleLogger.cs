using Cycodlib.Abstractions;

namespace Cycod.Implementation
{
    /// <summary>
    /// Console implementation of ILogger for the console application
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        public void WriteDebug(string message)
        {
            ConsoleHelpers.WriteDebugLine(message);
        }

        public void WriteWarning(string message)
        {
            ConsoleHelpers.WriteWarning(message);
        }

        public void WriteLine(string message, bool overrideQuiet = false)
        {
            ConsoleHelpers.WriteLine(message, null, overrideQuiet);
        }

        public void WriteError(string message)
        {
            ConsoleHelpers.WriteError(message);
        }

        public bool IsDebugEnabled => ConsoleHelpers.IsDebug();

        public bool IsQuietMode => ConsoleHelpers.IsQuiet();
    }
}