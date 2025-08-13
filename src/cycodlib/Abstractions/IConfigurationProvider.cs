using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cycodlib.Abstractions
{
    /// <summary>
    /// Provides configuration access abstraction for platform-agnostic configuration management
    /// </summary>
    public interface IConfigurationProvider
    {
        /// <summary>
        /// Gets an environment variable value
        /// </summary>
        /// <param name="name">The name of the environment variable</param>
        /// <returns>The value of the environment variable, or null if not found</returns>
        string? GetEnvironmentVariable(string name);

        /// <summary>
        /// Gets a configuration value by key
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <returns>The configuration value, or null if not found</returns>
        string? GetConfigValue(string key);

        /// <summary>
        /// Gets a configuration value as an integer
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">The default value if the key is not found or cannot be parsed</param>
        /// <returns>The configuration value as an integer</returns>
        int GetConfigValueAsInt(string key, int defaultValue = 0);

        /// <summary>
        /// Gets a configuration value as a boolean
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">The default value if the key is not found or cannot be parsed</param>
        /// <returns>The configuration value as a boolean</returns>
        bool GetConfigValueAsBool(string key, bool defaultValue = false);

        /// <summary>
        /// Saves a configuration value
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="value">The configuration value</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task SaveConfigValueAsync(string key, string value);

        /// <summary>
        /// Removes a configuration value
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task RemoveConfigValueAsync(string key);

        /// <summary>
        /// Gets all configuration keys
        /// </summary>
        /// <returns>An enumerable of all configuration keys</returns>
        Task<IEnumerable<string>> GetAllKeysAsync();
    }
}