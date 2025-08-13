using Cycodlib.Abstractions;

namespace Cycod.Implementation
{
    /// <summary>
    /// Console implementation of IConfigurationProvider using existing cycod configuration system
    /// </summary>
    public class ConsoleConfigurationProvider : IConfigurationProvider
    {
        public string? GetEnvironmentVariable(string name)
        {
            return EnvironmentHelpers.FindEnvVar(name);
        }

        public string? GetConfigValue(string key)
        {
            return ConfigStore.Instance.GetFromAnyScope(key)?.Value?.ToString();
        }

        public int GetConfigValueAsInt(string key, int defaultValue = 0)
        {
            var value = GetConfigValue(key);
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        public bool GetConfigValueAsBool(string key, bool defaultValue = false)
        {
            var value = GetConfigValue(key);
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }

        public async Task SaveConfigValueAsync(string key, string value)
        {
            // The existing ConfigStore.Instance might not have async save operations
            // This would need to be implemented based on the actual ConfigStore API
            await Task.Run(() => {
                // TODO: Implement async save operation
                // This might require calling ConfigStore methods or file operations
            });
        }

        public async Task RemoveConfigValueAsync(string key)
        {
            // The existing ConfigStore.Instance might not have async remove operations
            // This would need to be implemented based on the actual ConfigStore API
            await Task.Run(() => {
                // TODO: Implement async remove operation
                // This might require calling ConfigStore methods
            });
        }

        public async Task<IEnumerable<string>> GetAllKeysAsync()
        {
            // The existing ConfigStore.Instance might not have a method to get all keys
            // This would need to be implemented based on the actual ConfigStore API
            return await Task.Run(() => {
                // TODO: Implement get all keys operation
                // This might require calling ConfigStore methods
                return Array.Empty<string>();
            });
        }
    }
}