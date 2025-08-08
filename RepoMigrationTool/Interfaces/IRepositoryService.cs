namespace RepoMigrationTool.Interfaces;
 public interface IRepositoryService
    {
        Task<string> CloneRepositoryAsync(string sourceUrl, LibGit2Sharp.Credentials credentials);
        Task PullAllBranchesAsync(string repositoryPath);
        Task RenameDefaultBranchAsync(string repositoryPath, string currentBranch, string newBranch);
        Task SetRemoteUrlAsync(string repositoryPath, string remoteName, string newUrl);
        Task MergeRemoteMainAsync(string repositoryPath, string commitMessage, LibGit2Sharp.Credentials credentials);
        Task PushAllBranchesAsync(string repositoryPath, LibGit2Sharp.Credentials credentials);
        Task PushAllTagsAsync(string repositoryPath, LibGit2Sharp.Credentials credentials);
    }