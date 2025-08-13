using Cycodlib.Abstractions;

namespace Cycod.Implementation
{
    /// <summary>
    /// File system implementation of IStorageProvider for the console application
    /// </summary>
    public class FileStorageProvider : IStorageProvider
    {
        public async Task<string?> ReadTextAsync(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    return await File.ReadAllTextAsync(path);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task WriteTextAsync(string path, string content)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            await File.WriteAllTextAsync(path, content);
        }

        public async Task AppendTextAsync(string path, string content)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            await File.AppendAllTextAsync(path, content);
        }

        public Task<bool> ExistsAsync(string path)
        {
            return Task.FromResult(File.Exists(path));
        }

        public Task<IEnumerable<string>> ListFilesAsync(string directory, string pattern = "*")
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    var files = Directory.GetFiles(directory, pattern);
                    return Task.FromResult<IEnumerable<string>>(files);
                }
                return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
            }
            catch
            {
                return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
            }
        }

        public Task DeleteAsync(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Ignore deletion errors
            }
            
            return Task.CompletedTask;
        }

        public Task CreateDirectoryAsync(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch
            {
                // Ignore directory creation errors
            }
            
            return Task.CompletedTask;
        }

        public Task<StorageItemInfo?> GetItemInfoAsync(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    var info = new StorageItemInfo
                    {
                        FullPath = fileInfo.FullName,
                        Name = fileInfo.Name,
                        IsDirectory = false,
                        Size = fileInfo.Length,
                        LastModified = fileInfo.LastWriteTime
                    };
                    return Task.FromResult<StorageItemInfo?>(info);
                }
                
                if (Directory.Exists(path))
                {
                    var directoryInfo = new DirectoryInfo(path);
                    var info = new StorageItemInfo
                    {
                        FullPath = directoryInfo.FullName,
                        Name = directoryInfo.Name,
                        IsDirectory = true,
                        Size = 0,
                        LastModified = directoryInfo.LastWriteTime
                    };
                    return Task.FromResult<StorageItemInfo?>(info);
                }
                
                return Task.FromResult<StorageItemInfo?>(null);
            }
            catch
            {
                return Task.FromResult<StorageItemInfo?>(null);
            }
        }
    }
}