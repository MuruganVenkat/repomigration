namespace RepositoryMigration.Models
{
    public class RepositoryInfo
    {
        public string SourceUrl { get; set; } = string.Empty;
        public string DestinationUrl { get; set; } = string.Empty;
        public string CommitMessage { get; set; } = string.Empty;
        public string? WorkingDirectory { get; set; }
    }
}