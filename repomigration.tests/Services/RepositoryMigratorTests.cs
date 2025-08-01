using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using RepositoryMigration.Interfaces;
using RepositoryMigration.Models;
using RepositoryMigration.Services;

namespace RepositoryMigration.Tests.Services
{
    public class RepositoryMigratorTests
    {
        private readonly Mock<IGitCommandExecutor> _mockGitExecutor;
        private readonly Mock<IYamlConfigReader> _mockConfigReader;
        private readonly Mock<ILogger<RepositoryMigrator>> _mockLogger;
        private readonly RepositoryMigrator _repositoryMigrator;

        public RepositoryMigratorTests()
        {
            _mockGitExecutor = new Mock<IGitCommandExecutor>();
            _mockConfigReader = new Mock<IYamlConfigReader>();
            _mockLogger = new Mock<ILogger<RepositoryMigrator>>();
            _repositoryMigrator = new RepositoryMigrator(_mockGitExecutor.Object, _mockConfigReader.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task MigrateRepositoryAsync_WithValidRepository_ReturnsTrue()
        {
            // Arrange
            var repositoryInfo = new RepositoryInfo
            {
                SourceUrl = "https://dev.azure.com/org/project/_git/test-repo",
                DestinationUrl = "https://github.com/org/test-repo.git",
                CommitMessage = "Migration commit"
            };

            _mockGitExecutor.Setup(x => x.CloneRepositoryAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockGitExecutor.Setup(x => x.PullAllBranchesAsync(It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockGitExecutor.Setup(x => x.RenameDefaultBranchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockGitExecutor.Setup(x => x.SetRemoteUrlAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockGitExecutor.Setup(x => x.MergeRemoteMainAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockGitExecutor.Setup(x => x.PushAllBranchesAsync(It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockGitExecutor.Setup(x => x.PushAllTagsAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _repositoryMigrator.MigrateRepositoryAsync(repositoryInfo);

            // Assert
            Assert.True(result);
            _mockGitExecutor.Verify(x => x.CloneRepositoryAsync(repositoryInfo.SourceUrl, It.IsAny<string>()), Times.Once);
            _mockGitExecutor.Verify(x => x.SetRemoteUrlAsync(It.IsAny<string>(), repositoryInfo.DestinationUrl), Times.Once);
            _mockGitExecutor.Verify(x => x.MergeRemoteMainAsync(It.IsAny<string>(), repositoryInfo.CommitMessage), Times.Once);
        }

        [Fact]
        public async Task MigrateRepositoryAsync_WithNullRepository_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _repositoryMigrator.MigrateRepositoryAsync(null));
        }

        [Fact]
        public async Task MigrateRepositoryAsync_WithEmptySourceUrl_ThrowsArgumentException()
        {
            // Arrange
            var repositoryInfo = new RepositoryInfo
            {
                SourceUrl = "",
                DestinationUrl = "https://github.com/org/test-repo.git"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _repositoryMigrator.MigrateRepositoryAsync(repositoryInfo));
        }

        [Fact]
        public async Task MigrateRepositoryAsync_WhenCloneFails_ReturnsFalse()
        {
            // Arrange
            var repositoryInfo = new RepositoryInfo
            {
                SourceUrl = "https://dev.azure.com/org/project/_git/test-repo",
                DestinationUrl = "https://github.com/org/test-repo.git"
            };

            _mockGitExecutor.Setup(x => x.CloneRepositoryAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var result = await _repositoryMigrator.MigrateRepositoryAsync(repositoryInfo);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task MigrateAllRepositoriesAsync_WithValidConfig_CallsMigrateForEachRepository()
        {
            // Arrange
            var configPath = "test-config.yaml";
            var config = new MigrationConfig
            {
                Repositories = new List<RepositoryInfo>
                {
                    new RepositoryInfo { SourceUrl = "https://dev.azure.com/org/project/_git/test-repo1", DestinationUrl = "https://github.com/org/test-repo1.git" },
                    new RepositoryInfo { SourceUrl = "https://dev.azure.com/org/project/_git/test-repo2", DestinationUrl = "https://github.com/org/test-repo2.git" }
                }
            };

            _mockConfigReader.Setup(x => x.ReadConfigAsync(configPath))
                .ReturnsAsync(config);

            // Mock all git operations to succeed
            _mockGitExecutor.Setup(x => x.CloneRepositoryAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockGitExecutor.Setup(x => x.PullAllBranchesAsync(It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockGitExecutor.Setup(x => x.RenameDefaultBranchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockGitExecutor.Setup(x => x.SetRemoteUrlAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockGitExecutor.Setup(x => x.MergeRemoteMainAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockGitExecutor.Setup(x => x.PushAllBranchesAsync(It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockGitExecutor.Setup(x => x.PushAllTagsAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            await _repositoryMigrator.MigrateAllRepositoriesAsync(configPath);

            // Assert
            _mockConfigReader.Verify(x => x.ReadConfigAsync(configPath), Times.Once);
            _mockGitExecutor.Verify(x => x.CloneRepositoryAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }
    }
}