
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RepoMigrationTool.Interfaces;
using RepoMigrationTool.Models;
using RepoMigrationTool.Services;
using System.ComponentModel.DataAnnotations;
namespace RepoMigrationTool.Tests;

[TestClass]
    public class ConfigurationServiceTests
    {
        private Mock<IFileSystemService> _fileSystemMock = null!;
        private Mock<ILoggerService> _loggerMock = null!;
        private ConfigurationService _configurationService = null!;

        [TestInitialize]
        public void Setup()
        {
            _fileSystemMock = new Mock<IFileSystemService>();
            _loggerMock = new Mock<ILoggerService>();
            _configurationService = new ConfigurationService(_fileSystemMock.Object, _loggerMock.Object);
        }

        [TestMethod]
        public async Task LoadConfigurationAsync_ValidYaml_ReturnsConfig()
        {
            // Arrange
            var yamlContent = @"
repositories:
  - name: ""test-repo""
    sourceUrl: ""https://dev.azure.com/test/test/_git/test""
    destinationUrl: ""https://github.com/test/test.git""
    commitMessage: ""Test migration""
    defaultBranch: ""main""
    migrateTags: true
    migrateAllBranches: true";

            _fileSystemMock.Setup(x => x.ReadAllTextAsync("test.yaml"))
                .ReturnsAsync(yamlContent);

            // Act
            var result = await _configurationService.LoadConfigurationAsync("test.yaml");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Repositories.Count);
            Assert.AreEqual("test-repo", result.Repositories[0].Name);
            Assert.AreEqual("https://dev.azure.com/test/test/_git/test", result.Repositories[0].SourceUrl);
        }

        [TestMethod]
        public async Task LoadConfigurationAsync_EmptyRepositories_ThrowsException()
        {
            // Arrange
            var yamlContent = "repositories: []";
            _fileSystemMock.Setup(x => x.ReadAllTextAsync("test.yaml"))
                .ReturnsAsync(yamlContent);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _configurationService.LoadConfigurationAsync("test.yaml"));
        }

        [TestMethod]
        public async Task LoadConfigurationAsync_MissingRequiredFields_ThrowsException()
        {
            // Arrange
            var yamlContent = @"
repositories:
  - name: ""test-repo""
    sourceUrl: """"
    destinationUrl: ""https://github.com/test/test.git""
    commitMessage: ""Test migration""";

            _fileSystemMock.Setup(x => x.ReadAllTextAsync("test.yaml"))
                .ReturnsAsync(yamlContent);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _configurationService.LoadConfigurationAsync("test.yaml"));
        }
    }

    [TestClass]
    public class CredentialServiceTests
    {
        private Mock<ILoggerService> _loggerMock = null!;
        private CredentialService _credentialService = null!;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILoggerService>();
            _credentialService = new CredentialService(_loggerMock.Object);
        }

        [TestMethod]
        public void GetAdoToken_WhenEnvironmentVariableSet_ReturnsToken()
        {
            // Arrange
            Environment.SetEnvironmentVariable("ADO_TOKEN", "test-ado-token");

            // Act
            var result = _credentialService.GetAdoToken();

            // Assert
            Assert.AreEqual("test-ado-token", result);

            // Cleanup
            Environment.SetEnvironmentVariable("ADO_TOKEN", null);
        }

        [TestMethod]
        public void GetAdoToken_WhenEnvironmentVariableNotSet_ThrowsException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("ADO_TOKEN", null);

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() => _credentialService.GetAdoToken());
        }

        [TestMethod]
        public void GetGitHubToken_WhenEnvironmentVariableSet_ReturnsToken()
        {
            // Arrange
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", "test-github-token");

            // Act
            var result = _credentialService.GetGitHubToken();

            // Assert
            Assert.AreEqual("test-github-token", result);

            // Cleanup
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
        }

        [TestMethod]
        public void GetCredentials_AzureDevOpsUrl_ReturnsCorrectCredentials()
        {
            // Arrange
            Environment.SetEnvironmentVariable("ADO_TOKEN", "test-ado-token");
            var url = "https://dev.azure.com/test/test/_git/test";

            // Act
            var credentials = _credentialService.GetCredentials(url);

            // Assert
            Assert.IsNotNull(credentials);
            
            // Cleanup
            Environment.SetEnvironmentVariable("ADO_TOKEN", null);
        }

        [TestMethod]
        public void GetCredentials_GitHubUrl_ReturnsCorrectCredentials()
        {
            // Arrange
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", "test-github-token");
            var url = "https://github.com/test/test.git";

            // Act
            var credentials = _credentialService.GetCredentials(url);

            // Assert
            Assert.IsNotNull(credentials);
            
            // Cleanup
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
        }

        [TestMethod]
        public void GetCredentials_UnsupportedUrl_ThrowsException()
        {
            // Arrange
            var url = "https://unsupported.com/test/test.git";

            // Act & Assert
            Assert.ThrowsException<NotSupportedException>(() => _credentialService.GetCredentials(url));
        }
    }

    [TestClass]
    public class MigrationServiceTests
    {
        private Mock<IConfigurationService> _configServiceMock = null!;
        private Mock<IRepositoryService> _repositoryServiceMock = null!;
        private Mock<ICredentialService> _credentialServiceMock = null!;
        private Mock<IFileSystemService> _fileSystemServiceMock = null!;
        private Mock<ILoggerService> _loggerMock = null!;
        private MigrationService _migrationService = null!;

        [TestInitialize]
        public void Setup()
        {
            _configServiceMock = new Mock<IConfigurationService>();
            _repositoryServiceMock = new Mock<IRepositoryService>();
            _credentialServiceMock = new Mock<ICredentialService>();
            _fileSystemServiceMock = new Mock<IFileSystemService>();
            _loggerMock = new Mock<ILoggerService>();
            
            _migrationService = new MigrationService(
                _configServiceMock.Object,
                _repositoryServiceMock.Object,
                _credentialServiceMock.Object,
                _fileSystemServiceMock.Object,
                _loggerMock.Object);
        }

        [TestMethod]
        public async Task MigrateRepositoryAsync_SuccessfulMigration_ReturnsTrue()
        {
            // Arrange
            var migration = new RepositoryMigration
            {
                Name = "test-repo",
                SourceUrl = "https://dev.azure.com/test/test/_git/test",
                DestinationUrl = "https://github.com/test/test.git",
                CommitMessage = "Test migration",
                DefaultBranch = "main",
                MigrateAllBranches = true,
                MigrateTags = true
            };

            var mockCredentials = new Mock<LibGit2Sharp.Credentials>();
            var tempPath = "/tmp/test-repo";

            _credentialServiceMock.Setup(x => x.GetCredentials(migration.SourceUrl))
                .Returns(mockCredentials.Object);
            _credentialServiceMock.Setup(x => x.GetCredentials(migration.DestinationUrl))
                .Returns(mockCredentials.Object);
            _repositoryServiceMock.Setup(x => x.CloneRepositoryAsync(migration.SourceUrl, mockCredentials.Object))
                .ReturnsAsync(tempPath);
            _fileSystemServiceMock.Setup(x => x.DirectoryExists(tempPath)).Returns(true);

            // Act
            var result = await _migrationService.MigrateRepositoryAsync(migration);

            // Assert
            Assert.IsTrue(result);
            _repositoryServiceMock.Verify(x => x.CloneRepositoryAsync(migration.SourceUrl, mockCredentials.Object), Times.Once);
            _repositoryServiceMock.Verify(x => x.PullAllBranchesAsync(tempPath), Times.Once);
            _repositoryServiceMock.Verify(x => x.SetRemoteUrlAsync(tempPath, "origin", migration.DestinationUrl), Times.Once);
            _repositoryServiceMock.Verify(x => x.MergeRemoteMainAsync(tempPath, migration.CommitMessage, mockCredentials.Object), Times.Once);
            _repositoryServiceMock.Verify(x => x.PushAllBranchesAsync(tempPath, mockCredentials.Object), Times.Once);
            _repositoryServiceMock.Verify(x => x.PushAllTagsAsync(tempPath, mockCredentials.Object), Times.Once);
            _fileSystemServiceMock.Verify(x => x.DeleteDirectory(tempPath), Times.Once);
        }

        [TestMethod]
        public async Task MigrateRepositoryAsync_CloneFailure_ReturnsFalse()
        {
            // Arrange
            var migration = new RepositoryMigration
            {
                Name = "test-repo",
                SourceUrl = "https://dev.azure.com/test/test/_git/test",
                DestinationUrl = "https://github.com/test/test.git",
                CommitMessage = "Test migration"
            };

            var mockCredentials = new Mock<LibGit2Sharp.Credentials>();
            
            _credentialServiceMock.Setup(x => x.GetCredentials(migration.SourceUrl))
                .Returns(mockCredentials.Object);
            _repositoryServiceMock.Setup(x => x.CloneRepositoryAsync(migration.SourceUrl, mockCredentials.Object))
                .ThrowsAsync(new InvalidOperationException("Clone failed"));

            // Act
            var result = await _migrationService.MigrateRepositoryAsync(migration);

            // Assert
            Assert.IsFalse(result);
            _loggerMock.Verify(x => x.LogError(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
        }

        [TestMethod]
        public async Task MigrateAllRepositoriesAsync_MultipleRepositories_ProcessesAll()
        {
            // Arrange
            var config = new MigrationConfig
            {
                Repositories = new List<RepositoryMigration>
                {
                    new RepositoryMigration
                    {
                        Name = "repo1",
                        SourceUrl = "https://dev.azure.com/test/test/_git/repo1",
                        DestinationUrl = "https://github.com/test/repo1.git",
                        CommitMessage = "Migration 1"
                    },
                    new RepositoryMigration
                    {
                        Name = "repo2",
                        SourceUrl = "https://dev.azure.com/test/test/_git/repo2",
                        DestinationUrl = "https://github.com/test/repo2.git",
                        CommitMessage = "Migration 2"
                    }
                }
            };

            var mockCredentials = new Mock<LibGit2Sharp.Credentials>();
            
            _configServiceMock.Setup(x => x.LoadConfigurationAsync("test.yaml"))
                .ReturnsAsync(config);
            _credentialServiceMock.Setup(x => x.GetCredentials(It.IsAny<string>()))
                .Returns(mockCredentials.Object);
            _repositoryServiceMock.Setup(x => x.CloneRepositoryAsync(It.IsAny<string>(), It.IsAny<LibGit2Sharp.Credentials>()))
                .ReturnsAsync("/tmp/test");
            _fileSystemServiceMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);

            // Act
            var results = await _migrationService.MigrateAllRepositoriesAsync("test.yaml");

            // Assert
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("repo1", results[0].RepositoryName);
            Assert.AreEqual("repo2", results[1].RepositoryName);
        }
    }

    [TestClass]
    public class FileSystemServiceTests
    {
        private FileSystemService _fileSystemService = null!;

        [TestInitialize]
        public void Setup()
        {
            _fileSystemService = new FileSystemService();
        }

        [TestMethod]
        public async Task ReadAllTextAsync_ValidFile_ReturnsContent()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var content = "test content";
            await File.WriteAllTextAsync(tempFile, content);

            try
            {
                // Act
                var result = await _fileSystemService.ReadAllTextAsync(tempFile);

                // Assert
                Assert.AreEqual(content, result);
            }
            finally
            {
                // Cleanup
                File.Delete(tempFile);
            }
        }

        [TestMethod]
        public void DirectoryExists_ExistingDirectory_ReturnsTrue()
        {
            // Arrange
            var tempDir = Path.GetTempPath();

            // Act
            var result = _fileSystemService.DirectoryExists(tempDir);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DirectoryExists_NonExistentDirectory_ReturnsFalse()
        {
            // Arrange
            var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            // Act
            var result = _fileSystemService.DirectoryExists(nonExistentDir);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CreateTempDirectory_CreatesUniqueDirectory()
        {
            // Act
            var dir1 = _fileSystemService.CreateTempDirectory();
            var dir2 = _fileSystemService.CreateTempDirectory();

            try
            {
                // Assert
                Assert.IsTrue(Directory.Exists(dir1));
                Assert.IsTrue(Directory.Exists(dir2));
                Assert.AreNotEqual(dir1, dir2);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(dir1)) Directory.Delete(dir1);
                if (Directory.Exists(dir2)) Directory.Delete(dir2);
            }
        }
    }

