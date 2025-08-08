using RepoMigrationTool.Models;
namespace RepoMigrationTool.Interfaces;
 public interface IConfigurationService
    {
        Task<MigrationConfig> LoadConfigurationAsync(string configPath);
    }