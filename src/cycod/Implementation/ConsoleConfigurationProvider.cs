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
            await Task.Run(() => {
                try
                {
                    // Use the existing ConfigStore to save the value in user scope
                    ConfigStore.Instance.Set(key, value, ConfigFileScope.User);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to save config value '{key}': {ex.Message}", ex);
                }
            });
        }

        public async Task RemoveConfigValueAsync(string key)
        {
            await Task.Run(() => {
                try
                {
                    // Use the existing ConfigStore to remove the value from user scope
                    ConfigStore.Instance.Clear(key, ConfigFileScope.User);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to remove config value '{key}': {ex.Message}", ex);
                }
            });
        }

        public async Task<IEnumerable<string>> GetAllKeysAsync()
        {
            return await Task.Run(() => {
                try
                {
                    // Get all keys from all scopes
                    var userKeys = GetKeysFromScope(ConfigFileScope.User);
                    var projectKeys = GetKeysFromScope(ConfigFileScope.Local);
                    var globalKeys = GetKeysFromScope(ConfigFileScope.Global);
                    
                    return userKeys.Concat(projectKeys).Concat(globalKeys).Distinct();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to get all config keys: {ex.Message}", ex);
                }
            });
        }

        private static IEnumerable<string> GetKeysFromScope(ConfigFileScope scope)
        {
            try
            {
                // This would need to be implemented based on actual ConfigStore internals
                // For now, return empty collection as the ConfigStore doesn't expose key enumeration
                return Array.Empty<string>();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}