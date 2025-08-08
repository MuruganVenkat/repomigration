using RepoMigrationTool.Interfaces;
using RepoMigrationTool.Models;

namespace RepoMigrationTool.Services;
public class FileSystemService : IFileSystemService
    {
        public async Task<string> ReadAllTextAsync(string path)
        {
            return await File.ReadAllTextAsync(path);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }

        public string CreateTempDirectory()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"repo_migration_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempPath);
            return tempPath;
        }
    }