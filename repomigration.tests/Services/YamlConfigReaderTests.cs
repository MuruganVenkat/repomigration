using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using RepositoryMigration.Services;

namespace RepositoryMigration.Tests.Services
{
    public class YamlConfigReaderTests
    {
        private readonly YamlConfigReader _yamlConfigReader;

        public YamlConfigReaderTests()
        {
            _yamlConfigReader = new YamlConfigReader();
        }

        [Fact]
        public async Task ReadConfigAsync_WithValidYaml_ReturnsConfig()
        {
            // Arrange
            var yamlContent = @"
repositories:
  - sourceUrl: https://dev.azure.com/org/project/_git/repo1
    destinationUrl: https://github.com/org/repo1.git
    commitMessage: Migration commit for repo1
  - sourceUrl: https://dev.azure.com/org/project/_git/repo2
    destinationUrl: https://github.com/org/repo2.git
    commitMessage: Migration commit for repo2
";
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, yamlContent);

            try
            {
                // Act
                var config = await _yamlConfigReader.ReadConfigAsync(tempFile);

                // Assert
                Assert.NotNull(config);
                Assert.Equal(2, config.Repositories.Count);
                Assert.Equal("https://dev.azure.com/org/project/_git/repo1", config.Repositories[0].SourceUrl);
                Assert.Equal("https://github.com/org/repo1.git", config.Repositories[0].DestinationUrl);
                Assert.Equal("Migration commit for repo1", config.Repositories[0].CommitMessage);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task ReadConfigAsync_WithNonExistentFile_ThrowsFileNotFoundException()
        {
            // Arrange
            var nonExistentFile = "non-existent-file.yaml";

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => 
                _yamlConfigReader.ReadConfigAsync(nonExistentFile));
        }

        [Fact]
        public async Task ReadConfigAsync_WithInvalidYaml_ThrowsInvalidOperationException()
        {
            // Arrange
            var invalidYaml = "invalid: yaml: content: [";
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, invalidYaml);

            try
            {
                // Act & Assert
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    _yamlConfigReader.ReadConfigAsync(tempFile));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}