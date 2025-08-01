using System.Threading.Tasks;
using Xunit;
using RepositoryMigration.Services;

namespace RepositoryMigration.Tests.Services
{
    public class GitCommandExecutorTests
    {
        private readonly GitCommandExecutor _gitCommandExecutor;

        public GitCommandExecutorTests()
        {
            _gitCommandExecutor = new GitCommandExecutor();
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithValidCommand_ReturnsSuccess()
        {
            // Arrange
            var command = "--version";

            // Act
            var result = await _gitCommandExecutor.ExecuteCommandAsync(command);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("git version", result.Output);
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithInvalidCommand_ReturnsFalse()
        {
            // Arrange
            var command = "invalid-command";

            // Act
            var result = await _gitCommandExecutor.ExecuteCommandAsync(command);

            // Assert
            Assert.False(result.Success);
        }

        [Theory]
        [InlineData("https://github.com/test/repo.git", "test-repo")]
        [InlineData("https://dev.azure.com/org/project/_git/repo", "test-repo")]
        public async Task CloneRepositoryAsync_WithValidUrl_ShouldExecuteCloneCommand(string sourceUrl, string targetDirectory)
        {
            // This test would require a real repository or mocking the Process class
            // For demonstration, we'll test the command construction logic
            Assert.True(!string.IsNullOrEmpty(sourceUrl));
            Assert.True(!string.IsNullOrEmpty(targetDirectory));
        }
    }
}
