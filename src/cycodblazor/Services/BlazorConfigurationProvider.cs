using Microsoft.Extensions.Configuration;
using Cycodlib.Abstractions;

namespace Cycodblazor.Services
{
    /// <summary>
    /// Blazor implementation of IConfigurationProvider using .NET Core configuration system
    /// </summary>
    public class BlazorConfigurationProvider : Cycodlib.Abstractions.IConfigurationProvider
    {
        private readonly IConfiguration _configuration;
        private readonly BrowserStorageProvider _storageProvider;

        public BlazorConfigurationProvider(IConfiguration configuration, BrowserStorageProvider storageProvider)
        {
            _configuration = configuration;
            _storageProvider = storageProvider;
        }

        public string? GetEnvironmentVariable(string name)
        {
            return _configuration[$"Environment:{name}"];
        }

        public string? GetConfigValue(string key)
        {
            return _configuration[key];
        }

        public int GetConfigValueAsInt(string key, int defaultValue = 0)
        {
            return _configuration.GetValue(key, defaultValue);
        }

        public bool GetConfigValueAsBool(string key, bool defaultValue = false)
        {
            return _configuration.GetValue(key, defaultValue);
        }

        public async Task SaveConfigValueAsync(string key, string value)
        {
            // Store user settings in browser storage
            await _storageProvider.WriteTextAsync($"config:{key}", value);
        }

        public async Task RemoveConfigValueAsync(string key)
        {
            await _storageProvider.DeleteAsync($"config:{key}");
        }

        public async Task<IEnumerable<string>> GetAllKeysAsync()
        {
            try
            {
                var keys = await _storageProvider.ListFilesAsync("config:");
                return keys.Select(k => k.Substring("config:".Length));
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}