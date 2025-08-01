using System.Threading.Tasks;

namespace RepositoryMigration.Interfaces
{
    public interface IGitCommandExecutor
    {
        Task<(bool Success, string Output)> ExecuteCommandAsync(string command, string workingDirectory = "");
        Task<bool> CloneRepositoryAsync(string sourceUrl, string targetDirectory);
        Task<bool> PullAllBranchesAsync(string workingDirectory);
        Task<bool> RenameDefaultBranchAsync(string workingDirectory, string oldName = "main", string newName = "old-main");
        Task<bool> SetRemoteUrlAsync(string workingDirectory, string newRemoteUrl);
        Task<bool> MergeRemoteMainAsync(string workingDirectory, string commitMessage);
        Task<bool> PushAllBranchesAsync(string workingDirectory);
        Task<bool> PushAllTagsAsync(string workingDirectory);
    }
}