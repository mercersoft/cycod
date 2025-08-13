using Microsoft.JSInterop;
using System.Text.Json;
using Cycodlib.Abstractions;

namespace Cycodblazor.Services
{
    /// <summary>
    /// Browser implementation of IStorageProvider using localStorage
    /// </summary>
    public class BrowserStorageProvider : IStorageProvider
    {
        private readonly IJSRuntime _jsRuntime;
        
        public BrowserStorageProvider(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<string?> ReadTextAsync(string path)
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", path);
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
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", path, content);
            }
            catch
            {
                // Ignore storage errors
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
    }
}