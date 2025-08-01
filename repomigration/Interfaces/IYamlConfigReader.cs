using System.Threading.Tasks;
using RepositoryMigration.Models;

namespace RepositoryMigration.Interfaces
{
    public interface IYamlConfigReader
    {
        Task<MigrationConfig> ReadConfigAsync(string filePath);
    }
}