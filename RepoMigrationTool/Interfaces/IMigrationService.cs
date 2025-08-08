using RepoMigrationTool.Models;
namespace RepoMigrationTool.Interfaces;

public interface IMigrationService
{
    Task<bool> MigrateRepositoryAsync(RepositoryMigration migration);
    Task<List<MigrationResult>> MigrateAllRepositoriesAsync(string configPath);
}

 public class MigrationResult
{
    public string RepositoryName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CompletedAt { get; set; }
}