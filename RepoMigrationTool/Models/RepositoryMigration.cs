using System.ComponentModel.DataAnnotations;
using YamlDotNet.Serialization;

namespace RepoMigrationTool.Models
{
    public class RepositoryMigration
    {
         [Required]
        [YamlMember(Alias = "name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [YamlMember(Alias = "sourceUrl")]
        public string SourceUrl { get; set; } = string.Empty;

        [Required]
        [YamlMember(Alias = "destinationUrl")]
        public string DestinationUrl { get; set; } = string.Empty;

        [Required]
        [YamlMember(Alias = "commitMessage")]
        public string CommitMessage { get; set; } = string.Empty;

        [YamlMember(Alias = "defaultBranch")]
        public string? DefaultBranch { get; set; } = "main";
        
        [YamlMember(Alias = "migrateTags")]
        public bool MigrateTags { get; set; } = true;
        
        [YamlMember(Alias = "migrateAllBranches")]
        public bool MigrateAllBranches { get; set; } = true;
  
    }
}