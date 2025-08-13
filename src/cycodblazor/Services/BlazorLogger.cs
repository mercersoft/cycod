using Microsoft.JSInterop;
using Cycodlib.Abstractions;

namespace Cycodblazor.Services
{
    /// <summary>
    /// Blazor implementation of ILogger using browser console
    /// </summary>
    public class BlazorLogger : Cycodlib.Abstractions.ILogger
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly bool _isDebugEnabled;
        private readonly bool _isQuietMode;

        public BlazorLogger(IJSRuntime jsRuntime, bool isDebugEnabled = true, bool isQuietMode = false)
        {
            _jsRuntime = jsRuntime;
            _isDebugEnabled = isDebugEnabled;
            _isQuietMode = isQuietMode;
        }

        public void WriteDebug(string message)
        {
            if (_isDebugEnabled)
            {
                _ = Task.Run(async () => 
                {
                    try
                    {
                        await _jsRuntime.InvokeVoidAsync("console.log", $"[DEBUG] {message}");
                    }
                    catch
                    {
                        // Ignore JS interop errors
                    }
                });
            }
        }

        public void WriteWarning(string message)
        {
            _ = Task.Run(async () => 
            {
                try
                {
                    await _jsRuntime.InvokeVoidAsync("console.warn", $"[WARNING] {message}");
                }
                catch
                {
                    // Ignore JS interop errors
                }
            });
        }

        public void WriteLine(string message, bool overrideQuiet = false)
        {
            if (!_isQuietMode || overrideQuiet)
            {
                _ = Task.Run(async () => 
                {
                    try
                    {
                        await _jsRuntime.InvokeVoidAsync("console.log", message);
                    }
                    catch
                    {
                        // Ignore JS interop errors
                    }
                });
            }
        }

        public void WriteError(string message)
        {
            _ = Task.Run(async () => 
            {
                try
                {
                    await _jsRuntime.InvokeVoidAsync("console.error", $"[ERROR] {message}");
                }
                catch
                {
                    // Ignore JS interop errors
                }
            });
        }

        public bool IsDebugEnabled => _isDebugEnabled;

        public bool IsQuietMode => _isQuietMode;
    }
}