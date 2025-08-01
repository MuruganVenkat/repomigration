using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RepositoryMigration.Interfaces;
using RepositoryMigration.Models;

namespace RepositoryMigration.Services
{
    public class RepositoryMigrator : IRepositoryMigrator
    {
        private readonly IGitCommandExecutor _gitCommandExecutor;
        private readonly IYamlConfigReader _configReader;
        private readonly ILogger<RepositoryMigrator> _logger;

        public RepositoryMigrator(
            IGitCommandExecutor gitCommandExecutor,
            IYamlConfigReader configReader,
            ILogger<RepositoryMigrator> logger)
        {
            _gitCommandExecutor = gitCommandExecutor ?? throw new ArgumentNullException(nameof(gitCommandExecutor));
            _configReader = configReader ?? throw new ArgumentNullException(nameof(configReader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> MigrateRepositoryAsync(RepositoryInfo repositoryInfo)
        {
            if (repositoryInfo == null)
                throw new ArgumentNullException(nameof(repositoryInfo));

            if (string.IsNullOrEmpty(repositoryInfo.SourceUrl) || string.IsNullOrEmpty(repositoryInfo.DestinationUrl))
                throw new ArgumentException("Source and destination URLs are required");

            var repoName = GetRepositoryName(repositoryInfo.SourceUrl);
            var workingDirectory = repositoryInfo.WorkingDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), repoName);

            try
            {
                _logger.LogInformation($"Starting migration of {repositoryInfo.SourceUrl} to {repositoryInfo.DestinationUrl}");

                // Step 1: Clone repository
                _logger.LogInformation("Step 1: Cloning repository...");
                if (!await _gitCommandExecutor.CloneRepositoryAsync(repositoryInfo.SourceUrl, workingDirectory))
                {
                    _logger.LogError("Failed to clone repository");
                    return false;
                }

                // Step 2: Pull all branches
                _logger.LogInformation("Step 2: Pulling all branches...");
                if (!await _gitCommandExecutor.PullAllBranchesAsync(workingDirectory))
                {
                    _logger.LogWarning("Warning: Failed to pull all branches, continuing...");
                }

                // Step 3: Rename default branch
                _logger.LogInformation("Step 3: Renaming default branch...");
                if (!await _gitCommandExecutor.RenameDefaultBranchAsync(workingDirectory))
                {
                    _logger.LogWarning("Warning: Failed to rename default branch, continuing...");
                }

                // Step 4: Set new remote URL
                _logger.LogInformation("Step 4: Setting new remote URL...");
                if (!await _gitCommandExecutor.SetRemoteUrlAsync(workingDirectory, repositoryInfo.DestinationUrl))
                {
                    _logger.LogError("Failed to set new remote URL");
                    return false;
                }

                // Step 5: Merge remote main
                _logger.LogInformation("Step 5: Merging remote main...");
                var commitMessage = string.IsNullOrEmpty(repositoryInfo.CommitMessage) 
                    ? "Migration from ADO to GitHub" 
                    : repositoryInfo.CommitMessage;
                
                if (!await _gitCommandExecutor.MergeRemoteMainAsync(workingDirectory, commitMessage))
                {
                    _logger.LogWarning("Warning: Failed to merge remote main, continuing...");
                }

                // Step 6: Push all branches
                _logger.LogInformation("Step 6: Pushing all branches...");
                if (!await _gitCommandExecutor.PushAllBranchesAsync(workingDirectory))
                {
                    _logger.LogError("Failed to push all branches");
                    return false;
                }

                // Step 7: Push all tags
                _logger.LogInformation("Step 7: Pushing all tags...");
                if (!await _gitCommandExecutor.PushAllTagsAsync(workingDirectory))
                {
                    _logger.LogWarning("Warning: Failed to push all tags");
                }

                _logger.LogInformation($"Successfully migrated {repositoryInfo.SourceUrl} to {repositoryInfo.DestinationUrl}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error migrating repository: {ex.Message}");
                return false;
            }
            finally
            {
                // Cleanup: Remove working directory
                if (Directory.Exists(workingDirectory))
                {
                    try
                    {
                        Directory.Delete(workingDirectory, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to cleanup working directory: {workingDirectory}");
                    }
                }
            }
        }

        public async Task MigrateAllRepositoriesAsync(string configFilePath)
        {
            if (string.IsNullOrEmpty(configFilePath))
                throw new ArgumentException("Config file path is required", nameof(configFilePath));

            var config = await _configReader.ReadConfigAsync(configFilePath);
            
            _logger.LogInformation($"Found {config.Repositories.Count} repositories to migrate");

            foreach (var repo in config.Repositories)
            {
                await MigrateRepositoryAsync(repo);
            }
        }

        private static string GetRepositoryName(string url)
        {
            var uri = new Uri(url);
            var name = Path.GetFileNameWithoutExtension(uri.Segments.Last());
            return name;
        }
    }
}