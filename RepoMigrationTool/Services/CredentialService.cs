
using System.Text;
using LibGit2Sharp;
using RepoMigrationTool.Interfaces;
using RepoMigrationTool.Models;
using YamlDotNet.Serialization;
namespace RepoMigrationTool.Services;
public class CredentialService : ICredentialService
    {
        private readonly ILoggerService _logger;

        public CredentialService(ILoggerService logger)
        {
            _logger = logger;
        }

        public string GetAdoToken()
        {
            var token = Environment.GetEnvironmentVariable("ADO_TOKEN");
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("ADO_TOKEN environment variable is not set");
            return token;
        }

        public string GetGitHubToken()
        {
            var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("GITHUB_TOKEN environment variable is not set");
            return token;
        }

        public Credentials GetCredentials(string url)
        {
            try
            {
                if (url.Contains("dev.azure.com") || url.Contains("visualstudio.com"))
                {
                    var token = GetAdoToken();
                    return new UsernamePasswordCredentials { Username = "PAT", Password = token };
                }
                else if (url.Contains("github.com"))
                {
                    var token = GetGitHubToken();
                    return new UsernamePasswordCredentials { Username = "token", Password = token };
                }
                else
                {
                    throw new NotSupportedException($"Unsupported repository URL: {url}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get credentials for URL: {url}", ex);
                throw;
            }
        }
    }