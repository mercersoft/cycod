using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cycodlib.Abstractions
{
    /// <summary>
    /// Provides storage abstraction for platform-agnostic file operations
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>
        /// Reads text content from a storage location
        /// </summary>
        /// <param name="path">The path to read from</param>
        /// <returns>The text content, or null if the file doesn't exist</returns>
        Task<string?> ReadTextAsync(string path);

        /// <summary>
        /// Writes text content to a storage location
        /// </summary>
        /// <param name="path">The path to write to</param>
        /// <param name="content">The text content to write</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task WriteTextAsync(string path, string content);

        /// <summary>
        /// Appends text content to a storage location
        /// </summary>
        /// <param name="path">The path to append to</param>
        /// <param name="content">The text content to append</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task AppendTextAsync(string path, string content);

        /// <summary>
        /// Checks if a storage location exists
        /// </summary>
        /// <param name="path">The path to check</param>
        /// <returns>True if the location exists, false otherwise</returns>
        Task<bool> ExistsAsync(string path);

        /// <summary>
        /// Lists files in a directory matching a pattern
        /// </summary>
        /// <param name="directory">The directory to search in</param>
        /// <param name="pattern">The search pattern (e.g., "*.txt")</param>
        /// <returns>An enumerable of file paths</returns>
        Task<IEnumerable<string>> ListFilesAsync(string directory, string pattern = "*");

        /// <summary>
        /// Deletes a storage location
        /// </summary>
        /// <param name="path">The path to delete</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task DeleteAsync(string path);

        /// <summary>
        /// Creates a directory if it doesn't exist
        /// </summary>
        /// <param name="path">The directory path to create</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task CreateDirectoryAsync(string path);

        /// <summary>
        /// Gets information about a storage item
        /// </summary>
        /// <param name="path">The path to get information about</param>
        /// <returns>Storage item information, or null if the item doesn't exist</returns>
        Task<StorageItemInfo?> GetItemInfoAsync(string path);
    }

    /// <summary>
    /// Information about a storage item
    /// </summary>
    public class StorageItemInfo
    {
        /// <summary>
        /// Gets or sets the full path of the item
        /// </summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the item
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the item is a directory
        /// </summary>
        public bool IsDirectory { get; set; }

        /// <summary>
        /// Gets or sets the size of the item in bytes (0 for directories)
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the last modified date
        /// </summary>
        public DateTime LastModified { get; set; }
    }
}