using System;
using System.IO;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using RepositoryMigration.Interfaces;
using RepositoryMigration.Models;

namespace RepositoryMigration.Services
{
    public class YamlConfigReader : IYamlConfigReader
    {
        private readonly IDeserializer _deserializer;

        public YamlConfigReader()
        {
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
        }

        public async Task<MigrationConfig> ReadConfigAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Configuration file not found: {filePath}");

            try
            {
                var yamlContent = await File.ReadAllTextAsync(filePath);
                var config = _deserializer.Deserialize<MigrationConfig>(yamlContent);
                return config ?? new MigrationConfig();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse YAML configuration: {ex.Message}", ex);
            }
        }
    }
}