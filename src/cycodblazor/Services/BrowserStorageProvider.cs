using Microsoft.JSInterop;
using System.Text.Json;
using Cycodlib.Abstractions;

namespace Cycodblazor.Services
{
    /// <summary>
    /// Enhanced browser implementation of IStorageProvider with IndexedDB support for large data
    /// </summary>
    public class BrowserStorageProvider : IStorageProvider
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly int _localStorageMaxSize;
        private bool _indexedDbInitialized = false;
        
        public BrowserStorageProvider(IJSRuntime jsRuntime, int localStorageMaxSize = 1024 * 1024) // 1MB default
        {
            _jsRuntime = jsRuntime;
            _localStorageMaxSize = localStorageMaxSize;
        }

        public async Task<string?> ReadTextAsync(string path)
        {
            try
            {
                // Try localStorage first for small data
                var localData = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", path);
                if (localData != null)
                {
                    return localData;
                }

                // Try IndexedDB for larger data
                await EnsureIndexedDbInitializedAsync();
                return await _jsRuntime.InvokeAsync<string?>("indexedDbGet", "CycodStorage", path);
            }
            catch
            {
                return null;
            }
        }

        public async Task WriteTextAsync(string path, string content)
        {
            try
            {
                var contentSize = System.Text.Encoding.UTF8.GetByteCount(content);
                
                if (contentSize <= _localStorageMaxSize)
                {
                    // Use localStorage for small data
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", path, content);
                }
                else
                {
                    // Use IndexedDB for large data
                    await EnsureIndexedDbInitializedAsync();
                    await _jsRuntime.InvokeVoidAsync("indexedDbSet", "CycodStorage", path, content);
                    
                    // Remove from localStorage if it exists there
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", path);
                }
            }
            catch
            {
                // Fallback: try to store in localStorage anyway
                try
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", path, content);
                }
                catch
                {
                    // Ignore storage errors
                }
            }
        }

        public async Task AppendTextAsync(string path, string content)
        {
            try
            {
                var existing = await ReadTextAsync(path) ?? "";
                await WriteTextAsync(path, existing + content);
            }
            catch
            {
                // Ignore storage errors
            }
        }

        public async Task<bool> ExistsAsync(string path)
        {
            try
            {
                var item = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", path);
                return item != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<string>> ListFilesAsync(string directory, string pattern = "*")
        {
            try
            {
                var length = await _jsRuntime.InvokeAsync<int>("eval", "localStorage.length");
                var keys = new List<string>();
                
                for (int i = 0; i < length; i++)
                {
                    var key = await _jsRuntime.InvokeAsync<string>("localStorage.key", i);
                    if (key != null && key.StartsWith(directory))
                    {
                        keys.Add(key);
                    }
                }
                
                return keys;
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public async Task DeleteAsync(string path)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", path);
                
                // Also remove from IndexedDB if it exists there
                await EnsureIndexedDbInitializedAsync();
                await _jsRuntime.InvokeVoidAsync("indexedDbDelete", "CycodStorage", path);
            }
            catch
            {
                // Ignore deletion errors
            }
        }

        public Task CreateDirectoryAsync(string path)
        {
            // No concept of directories in localStorage
            return Task.CompletedTask;
        }

        public async Task<StorageItemInfo?> GetItemInfoAsync(string path)
        {
            try
            {
                var content = await ReadTextAsync(path);
                if (content != null)
                {
                    return new StorageItemInfo
                    {
                        FullPath = path,
                        Name = Path.GetFileName(path),
                        IsDirectory = false,
                        Size = System.Text.Encoding.UTF8.GetByteCount(content),
                        LastModified = DateTime.Now // localStorage doesn't track modification time
                    };
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task EnsureIndexedDbInitializedAsync()
        {
            if (!_indexedDbInitialized)
            {
                try
                {
                    await _jsRuntime.InvokeVoidAsync("initializeIndexedDb", "CycodStorage");
                    _indexedDbInitialized = true;
                }
                catch
                {
                    // IndexedDB initialization failed - will fallback to localStorage
                }
            }
        }
    }
}