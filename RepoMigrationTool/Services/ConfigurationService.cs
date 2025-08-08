
using System.Text;
using LibGit2Sharp;
using RepoMigrationTool.Interfaces;
using RepoMigrationTool.Models;
using YamlDotNet.Serialization;

namespace RepoMigrationTool.Services;

public class ConfigurationService : IConfigurationService
    {
        private readonly IFileSystemService _fileSystemService;
        private readonly ILoggerService _logger;

        public ConfigurationService(IFileSystemService fileSystemService, ILoggerService logger)
        {
            _fileSystemService = fileSystemService;
            _logger = logger;
        }

        public async Task<MigrationConfig> LoadConfigurationAsync(string configPath)
        {
            try
            {
                var yamlContent = await _fileSystemService.ReadAllTextAsync(configPath);
                
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();
                    
                var config = deserializer.Deserialize<MigrationConfig>(yamlContent);
                
                if (config == null)
                {
                    throw new InvalidOperationException("Failed to deserialize configuration file - result is null");
                }
                
                ValidateConfiguration(config);
                _logger.LogInfo($"Successfully loaded configuration with {config.Repositories.Count} repositories");
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load configuration from {configPath}", ex);
                throw;
            }
        }

        private void ValidateConfiguration(MigrationConfig config)
        {
            if (config.Repositories == null || !config.Repositories.Any())
                throw new ArgumentException("No repositories configured for migration");

            foreach (var repo in config.Repositories)
            {
                if (string.IsNullOrWhiteSpace(repo.Name))
                    throw new ArgumentException("Repository name is required");
                if (string.IsNullOrWhiteSpace(repo.SourceUrl))
                    throw new ArgumentException($"Source URL is required for repository: {repo.Name}");
                if (string.IsNullOrWhiteSpace(repo.DestinationUrl))
                    throw new ArgumentException($"Destination URL is required for repository: {repo.Name}");
                if (string.IsNullOrWhiteSpace(repo.CommitMessage))
                    throw new ArgumentException($"Commit message is required for repository: {repo.Name}");
            }
        }
    }