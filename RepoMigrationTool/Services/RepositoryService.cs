
using System.Text;
using LibGit2Sharp;
using RepoMigrationTool.Interfaces;
using RepoMigrationTool.Models;
using YamlDotNet.Serialization;
namespace RepoMigrationTool.Services;

public class RepositoryService : IRepositoryService
    {
        private readonly ICredentialService _credentialService;
        private readonly IFileSystemService _fileSystemService;
        private readonly ILoggerService _logger;

        public RepositoryService(ICredentialService credentialService, IFileSystemService fileSystemService, ILoggerService logger)
        {
            _credentialService = credentialService;
            _fileSystemService = fileSystemService;
            _logger = logger;
        }

        public async Task<string> CloneRepositoryAsync(string sourceUrl, Credentials credentials)
        {
            var tempDir = _fileSystemService.CreateTempDirectory();
            
            try
            {
                _logger.LogInfo($"Cloning repository from: {sourceUrl}");
                
                var cloneOptions = new CloneOptions
                {
                    IsBare = false,
                    Checkout = true
                };
                cloneOptions.FetchOptions.CredentialsProvider =new LibGit2Sharp.Handlers.CredentialsHandler(
                    (url, fromUrl, types) => credentials);
                await Task.Run(() => Repository.Clone(sourceUrl, tempDir, cloneOptions));
                _logger.LogInfo($"Repository cloned to: {tempDir}");
                return tempDir;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to clone repository from {sourceUrl}", ex);
                if (_fileSystemService.DirectoryExists(tempDir))
                    _fileSystemService.DeleteDirectory(tempDir);
                throw;
            }
        }

        public async Task PullAllBranchesAsync(string repositoryPath)
        {
            await Task.Run(() =>
            {
                using var repo = new Repository(repositoryPath);
                var credentials = _credentialService.GetCredentials(repo.Network.Remotes["origin"].Url);

                foreach (var remoteBranch in repo.Branches.Where(b => b.IsRemote))
                {
                    var localBranchName = remoteBranch.FriendlyName.Replace("origin/", "");
                    
                    if (localBranchName == "HEAD")
                        continue;

                    var localBranch = repo.Branches.FirstOrDefault(b => b.FriendlyName == localBranchName);
                    if (localBranch == null)
                    {
                        _logger.LogInfo($"Creating local branch: {localBranchName}");
                        repo.CreateBranch(localBranchName, remoteBranch.Tip);
                        localBranch = repo.Branches[localBranchName];
                        repo.Branches.Update(localBranch, b => b.TrackedBranch = remoteBranch.CanonicalName);
                    }
                }

                _logger.LogInfo("All remote branches pulled successfully");
            });
        }

        public async Task RenameDefaultBranchAsync(string repositoryPath, string currentBranch, string newBranch)
        {
            await Task.Run(() =>
            {
                using var repo = new Repository(repositoryPath);
                var branch = repo.Branches[currentBranch];
                
                if (branch != null)
                {
                    _logger.LogInfo($"Renaming branch {currentBranch} to {newBranch}");
                    repo.Branches.Rename(branch, newBranch);
                }
                else
                {
                    _logger.LogWarning($"Branch {currentBranch} not found, skipping rename");
                }
            });
        }

        public async Task SetRemoteUrlAsync(string repositoryPath, string remoteName, string newUrl)
        {
            await Task.Run(() =>
            {
                using var repo = new Repository(repositoryPath);
                var remote = repo.Network.Remotes[remoteName];
                
                if (remote != null)
                {
                    _logger.LogInfo($"Updating remote '{remoteName}' URL to: {newUrl}");
                    repo.Network.Remotes.Update(remoteName, r => r.Url = newUrl);
                }
                else
                {
                    _logger.LogInfo($"Adding new remote '{remoteName}' with URL: {newUrl}");
                    repo.Network.Remotes.Add(remoteName, newUrl);
                }
            });
        }

        public async Task MergeRemoteMainAsync(string repositoryPath, string commitMessage, Credentials credentials)
        {
            await Task.Run(() =>
            {
                using var repo = new Repository(repositoryPath);
                
                try
                {
                    var fetchOptions = new FetchOptions
                    {
                        CredentialsProvider = (_, _, _) => credentials
                    };

                    Commands.Fetch(repo, "origin", new string[0], fetchOptions, null);

                    var remoteBranch = repo.Branches["origin/main"];
                    if (remoteBranch != null)
                    {
                        var signature = new Signature("Migration Tool", "migration@tool.com", DateTimeOffset.Now);
                        var mergeOptions = new MergeOptions
                        {
                            FileConflictStrategy = CheckoutFileConflictStrategy.Theirs,
                            CommitOnSuccess = true
                        };

                        var mergeResult = repo.Merge(remoteBranch, signature, mergeOptions);
                        
                        if (mergeResult.Status == MergeStatus.Conflicts)
                        {
                            _logger.LogWarning("Merge conflicts detected, resolving automatically");
                            // Auto-resolve conflicts by taking theirs
                            foreach (var conflict in repo.Index.Conflicts)
                            {
                                repo.Index.Remove(conflict.Ancestor?.Path ?? conflict.Ours?.Path ?? conflict.Theirs.Path);
                                if (conflict.Theirs != null)
                                    repo.Index.Add(conflict.Theirs.Path);
                            }
                            
                            var commit = repo.Commit(commitMessage, signature, signature);
                            _logger.LogInfo($"Merge conflicts resolved and committed: {commit.Sha}");
                        }
                        else if (mergeResult.Status == MergeStatus.UpToDate)
                        {
                            _logger.LogInfo("Repository is already up to date");
                        }
                        else
                        {
                            _logger.LogInfo($"Merge completed successfully: {mergeResult.Status}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Remote main branch not found, skipping merge");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to merge remote main branch", ex);
                    throw;
                }
            });
        }

        public async Task PushAllBranchesAsync(string repositoryPath, Credentials credentials)
        {
            await Task.Run(() =>
            {
                using var repo = new Repository(repositoryPath);
                var pushOptions = new PushOptions
                {
                    CredentialsProvider = (_, _, _) => credentials
                };

                foreach (var branch in repo.Branches.Where(b => !b.IsRemote))
                {
                    try
                    {
                        _logger.LogInfo($"Pushing branch: {branch.FriendlyName}");
                        repo.Network.Push(repo.Network.Remotes["origin"], branch.CanonicalName, pushOptions);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to push branch {branch.FriendlyName}", ex);
                        throw;
                    }
                }

                _logger.LogInfo("All branches pushed successfully");
            });
        }

        public async Task PushAllTagsAsync(string repositoryPath, Credentials credentials)
        {
            await Task.Run(() =>
            {
                using var repo = new Repository(repositoryPath);
                var pushOptions = new PushOptions
                {
                    CredentialsProvider = (_, _, _) => credentials
                };

                if (repo.Tags.Any())
                {
                    try
                    {
                        _logger.LogInfo("Pushing all tags");
                        repo.Network.Push(repo.Network.Remotes["origin"], repo.Tags.Select(t => t.CanonicalName), pushOptions);
                        _logger.LogInfo("All tags pushed successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Failed to push tags", ex);
                        throw;
                    }
                }
                else
                {
                    _logger.LogInfo("No tags found to push");
                }
            });
        }
    }