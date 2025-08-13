using Cycodlib.Abstractions;
using System.Text;

namespace Cycod.Implementation
{
    /// <summary>
    /// Enhanced file system implementation of IStorageProvider with retry logic and validation
    /// </summary>
    public class FileStorageProvider : IStorageProvider
    {
        private readonly int _maxRetries;
        private readonly TimeSpan _retryDelay;
        private readonly ILogger? _logger;

        public FileStorageProvider(int maxRetries = 3, TimeSpan? retryDelay = null, ILogger? logger = null)
        {
            _maxRetries = maxRetries;
            _retryDelay = retryDelay ?? TimeSpan.FromMilliseconds(100);
            _logger = logger;
        }

        public async Task<string?> ReadTextAsync(string path)
        {
            if (!IsValidPath(path))
            {
                _logger?.WriteWarning($"Invalid path for read operation: {path}");
                return null;
            }

            return await RetryAsync(async () =>
            {
                if (File.Exists(path))
                {
                    return await File.ReadAllTextAsync(path, Encoding.UTF8);
                }
                return null;
            }, $"reading file {path}");
        }

        public async Task WriteTextAsync(string path, string content)
        {
            if (!IsValidPath(path))
            {
                _logger?.WriteWarning($"Invalid path for write operation: {path}");
                return;
            }

            await RetryAsync(async () =>
            {
                await EnsureDirectoryExistsAsync(path);
                await File.WriteAllTextAsync(path, content, Encoding.UTF8);
                return true;
            }, $"writing file {path}");
        }

        public async Task AppendTextAsync(string path, string content)
        {
            if (!IsValidPath(path))
            {
                _logger?.WriteWarning($"Invalid path for append operation: {path}");
                return;
            }

            await RetryAsync(async () =>
            {
                await EnsureDirectoryExistsAsync(path);
                await File.AppendAllTextAsync(path, content, Encoding.UTF8);
                return true;
            }, $"appending to file {path}");
        }

        public async Task<bool> ExistsAsync(string path)
        {
            if (!IsValidPath(path))
            {
                return false;
            }

            try
            {
                bool? result = await RetryAsync(async () =>
                {
                    await Task.Yield(); // Make async
                    return File.Exists(path);
                }, $"checking existence of {path}");
                return result ?? false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<string>> ListFilesAsync(string directory, string pattern = "*")
        {
            if (!IsValidPath(directory))
            {
                _logger?.WriteWarning($"Invalid directory path: {directory}");
                return Array.Empty<string>();
            }

            return await RetryAsync(async () =>
            {
                await Task.Yield(); // Make async
                if (Directory.Exists(directory))
                {
                    return Directory.GetFiles(directory, pattern).AsEnumerable();
                }
                return Array.Empty<string>().AsEnumerable();
            }, $"listing files in {directory}") ?? Array.Empty<string>();
        }

        public async Task DeleteAsync(string path)
        {
            if (!IsValidPath(path))
            {
                _logger?.WriteWarning($"Invalid path for delete operation: {path}");
                return;
            }

            await RetryAsync(async () =>
            {
                await Task.Yield(); // Make async
                if (File.Exists(path))
                {
                    File.Delete(path);
                    _logger?.WriteDebug($"Deleted file: {path}");
                }
                return true;
            }, $"deleting file {path}");
        }

        public async Task CreateDirectoryAsync(string path)
        {
            if (!IsValidPath(path))
            {
                _logger?.WriteWarning($"Invalid directory path: {path}");
                return;
            }

            await RetryAsync(async () =>
            {
                await Task.Yield(); // Make async
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    _logger?.WriteDebug($"Created directory: {path}");
                }
                return true;
            }, $"creating directory {path}");
        }

        public async Task<StorageItemInfo?> GetItemInfoAsync(string path)
        {
            if (!IsValidPath(path))
            {
                return null;
            }

            return await RetryAsync(async () =>
            {
                await Task.Yield(); // Make async
                
                if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    return new StorageItemInfo
                    {
                        FullPath = fileInfo.FullName,
                        Name = fileInfo.Name,
                        IsDirectory = false,
                        Size = fileInfo.Length,
                        LastModified = fileInfo.LastWriteTime
                    };
                }
                
                if (Directory.Exists(path))
                {
                    var directoryInfo = new DirectoryInfo(path);
                    return new StorageItemInfo
                    {
                        FullPath = directoryInfo.FullName,
                        Name = directoryInfo.Name,
                        IsDirectory = true,
                        Size = 0,
                        LastModified = directoryInfo.LastWriteTime
                    };
                }
                
                return null;
            }, $"getting info for {path}");
        }

        private async Task EnsureDirectoryExistsAsync(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                await CreateDirectoryAsync(directory);
            }
        }

        private static bool IsValidPath(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return false;

                // Check for invalid characters
                if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                    return false;

                // Additional security checks
                var fullPath = Path.GetFullPath(path);
                
                // Prevent directory traversal
                if (path.Contains("..") && !path.StartsWith(Environment.CurrentDirectory))
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<T?> RetryAsync<T>(Func<Task<T>> operation, string operationName)
        {
            Exception? lastException = null;

            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex) when (IsRetriableException(ex))
                {
                    lastException = ex;
                    _logger?.WriteDebug($"Attempt {attempt + 1} failed for {operationName}: {ex.Message}");

                    if (attempt < _maxRetries)
                    {
                        await Task.Delay(_retryDelay);
                    }
                }
                catch (Exception ex)
                {
                    // Non-retriable exceptions
                    _logger?.WriteError($"Non-retriable error for {operationName}: {ex.Message}");
                    throw;
                }
            }

            _logger?.WriteError($"All {_maxRetries + 1} attempts failed for {operationName}. Last error: {lastException?.Message}");
            return default;
        }

        private static bool IsRetriableException(Exception ex)
        {
            return ex is IOException ||
                   ex is UnauthorizedAccessException ||
                   ex is TimeoutException ||
                   ex is DirectoryNotFoundException;
        }
    }
}