using RepoMigrationTool.Interfaces;
using RepoMigrationTool.Models;

namespace RepoMigrationTool.Services;
public class MigrationService : IMigrationService
    {
        private readonly IConfigurationService _configurationService;
        private readonly IRepositoryService _repositoryService;
        private readonly ICredentialService _credentialService;
        private readonly IFileSystemService _fileSystemService;
        private readonly ILoggerService _logger;

        public MigrationService(
            IConfigurationService configurationService,
            IRepositoryService repositoryService,
            ICredentialService credentialService,
            IFileSystemService fileSystemService,
            ILoggerService logger)
        {
            _configurationService = configurationService;
            _repositoryService = repositoryService;
            _credentialService = credentialService;
            _fileSystemService = fileSystemService;
            _logger = logger;
        }

        public async Task<bool> MigrateRepositoryAsync(RepositoryMigration migration)
        {
            string? repositoryPath = null;
            
            try
            {
                _logger.LogInfo($"Starting migration for repository: {migration.Name}");

                // Step 1: Clone the source repository
                var sourceCredentials = _credentialService.GetCredentials(migration.SourceUrl);
                repositoryPath = await _repositoryService.CloneRepositoryAsync(migration.SourceUrl, sourceCredentials);

                // Step 2: Pull all branches
                if (migration.MigrateAllBranches)
                {
                    await _repositoryService.PullAllBranchesAsync(repositoryPath);
                }

                // Step 3: Rename default branch if needed
                if (!string.IsNullOrEmpty(migration.DefaultBranch) && migration.DefaultBranch != "main")
                {
                    await _repositoryService.RenameDefaultBranchAsync(repositoryPath, migration.DefaultBranch, "old-main");
                }

                // Step 4: Set new remote URL
                await _repositoryService.SetRemoteUrlAsync(repositoryPath, "origin", migration.DestinationUrl);

                // Step 5: Merge remote main
                var destinationCredentials = _credentialService.GetCredentials(migration.DestinationUrl);
                await _repositoryService.MergeRemoteMainAsync(repositoryPath, migration.CommitMessage, destinationCredentials);

                // Step 6: Push all branches
                if (migration.MigrateAllBranches)
                {
                    await _repositoryService.PushAllBranchesAsync(repositoryPath, destinationCredentials);
                }

                // Step 7: Push all tags
                if (migration.MigrateTags)
                {
                    await _repositoryService.PushAllTagsAsync(repositoryPath, destinationCredentials);
                }

                _logger.LogInfo($"Migration completed successfully for repository: {migration.Name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Migration failed for repository: {migration.Name}", ex);
                return false;
            }
            finally
            {
                // Cleanup temporary directory
                if (!string.IsNullOrEmpty(repositoryPath) && _fileSystemService.DirectoryExists(repositoryPath))
                {
                    try
                    {
                        _fileSystemService.DeleteDirectory(repositoryPath);
                        _logger.LogInfo($"Cleaned up temporary directory: {repositoryPath}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to cleanup temporary directory: {repositoryPath}. Error: {ex.Message}");
                    }
                }
            }
        }

        public async Task<List<MigrationResult>> MigrateAllRepositoriesAsync(string configPath)
        {
            var results = new List<MigrationResult>();
            
            try
            {
                var config = await _configurationService.LoadConfigurationAsync(configPath);
                _logger.LogInfo($"Loaded configuration with {config.Repositories.Count} repositories to migrate");

                foreach (var repository in config.Repositories)
                {
                    var startTime = DateTime.UtcNow;
                    var success = await MigrateRepositoryAsync(repository);
                    
                    results.Add(new MigrationResult
                    {
                        RepositoryName = repository.Name,
                        Success = success,
                        CompletedAt = DateTime.UtcNow,
                        ErrorMessage = success ? null : "Migration failed - check logs for details"
                    });

                    // Small delay between migrations to avoid overwhelming the systems
                    await Task.Delay(1000);
                }

                var successCount = results.Count(r => r.Success);
                _logger.LogInfo($"Migration batch completed. {successCount}/{results.Count} repositories migrated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to execute migration batch", ex);
                throw;
            }

            return results;
        }
    }