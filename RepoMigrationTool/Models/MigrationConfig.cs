using System.ComponentModel.DataAnnotations;
using YamlDotNet.Serialization;
namespace RepoMigrationTool.Models
{ public class MigrationConfig
    {
        [YamlMember(Alias = "repositories")]
        public List<RepositoryMigration> Repositories { get; set; } = new();
    }
}