using System.Threading.Tasks;
using RepositoryMigration.Models;

namespace RepositoryMigration.Interfaces
{
    public interface IRepositoryMigrator
    {
        Task<bool> MigrateRepositoryAsync(RepositoryInfo repositoryInfo);
        Task MigrateAllRepositoriesAsync(string configFilePath);
    }
}