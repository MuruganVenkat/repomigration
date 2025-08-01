using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using RepositoryMigration.Interfaces;

namespace RepositoryMigration.Services
{
    public class GitCommandExecutor : IGitCommandExecutor
    {
        public async Task<(bool Success, string Output)> ExecuteCommandAsync(string command, string workingDirectory = "")
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = command,
                    WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? Directory.GetCurrentDirectory() : workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                    return (false, "Failed to start git process");

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                var result = string.IsNullOrEmpty(error) ? output : $"{output}\n{error}";
                return (process.ExitCode == 0, result);
            }
            catch (Exception ex)
            {
                return (false, $"Exception: {ex.Message}");
            }
        }

        public async Task<bool> CloneRepositoryAsync(string sourceUrl, string targetDirectory)
        {
            var command = $"clone {sourceUrl} {targetDirectory}";
            var result = await ExecuteCommandAsync(command);
            return result.Success;
        }

        public async Task<bool> PullAllBranchesAsync(string workingDirectory)
        {
            // First, fetch all remote branches
            var fetchResult = await ExecuteCommandAsync("fetch --all", workingDirectory);
            if (!fetchResult.Success)
                return false;

            // Get list of remote branches
            var branchResult = await ExecuteCommandAsync("branch -r", workingDirectory);
            if (!branchResult.Success)
                return false;

            // Pull each remote branch
            var branches = branchResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var branch in branches)
            {
                var cleanBranch = branch.Trim().Replace("origin/", "");
                if (cleanBranch != "HEAD" && !cleanBranch.Contains("->"))
                {
                    await ExecuteCommandAsync($"checkout -b {cleanBranch} origin/{cleanBranch}", workingDirectory);
                }
            }

            return true;
        }

        public async Task<bool> RenameDefaultBranchAsync(string workingDirectory, string oldName = "main", string newName = "old-main")
        {
            var result = await ExecuteCommandAsync($"branch -m {oldName} {newName}", workingDirectory);
            return result.Success;
        }

        public async Task<bool> SetRemoteUrlAsync(string workingDirectory, string newRemoteUrl)
        {
            var result = await ExecuteCommandAsync($"remote set-url origin {newRemoteUrl}", workingDirectory);
            return result.Success;
        }

        public async Task<bool> MergeRemoteMainAsync(string workingDirectory, string commitMessage)
        {
            // Create a temporary file for the commit message
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, commitMessage);

            try
            {
                // Pull with merge commit
                var pullResult = await ExecuteCommandAsync("pull --no-rebase origin main --allow-unrelated-histories", workingDirectory);
                
                // If there are conflicts or merge needed, commit with the message
                if (!pullResult.Success || pullResult.Output.Contains("CONFLICT"))
                {
                    await ExecuteCommandAsync($"commit -F {tempFile}", workingDirectory);
                }

                return true;
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        public async Task<bool> PushAllBranchesAsync(string workingDirectory)
        {
            var result = await ExecuteCommandAsync("push --all origin", workingDirectory);
            return result.Success;
        }

        public async Task<bool> PushAllTagsAsync(string workingDirectory)
        {
            var result = await ExecuteCommandAsync("push --tags origin", workingDirectory);
            return result.Success;
        }
    }
}