namespace RepoMigrationTool.Interfaces;

 public interface IFileSystemService
    {
        Task<string> ReadAllTextAsync(string path);
        bool DirectoryExists(string path);
        void DeleteDirectory(string path);
        string CreateTempDirectory();
    }
