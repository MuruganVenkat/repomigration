namespace RepoMigrationTool.Interfaces;
 public interface ICredentialService
    {
        string GetAdoToken();
        string GetGitHubToken();
        LibGit2Sharp.Credentials GetCredentials(string url);
    }