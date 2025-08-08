using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RepoMigrationTool.Interfaces;
using RepoMigrationTool.Services;
namespace RepoMigrationTool;

 public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                var host = CreateHostBuilder(args).Build();
                
                using var scope = host.Services.CreateScope();
                var migrationService = scope.ServiceProvider.GetRequiredService<IMigrationService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILoggerService>();
                
                var configPath = args.Length > 0 ? args[0] : "migration-repo.yaml";
                
                if (!File.Exists(configPath))
                {
                    logger.LogError($"Configuration file not found: {configPath}");
                    return 1;
                }
                
                logger.LogInfo($"Starting repository migration using config: {configPath}");
                
                var results = await migrationService.MigrateAllRepositoriesAsync(configPath);
                
                // Log summary
                var successCount = results.Count(r => r.Success);
                var failureCount = results.Count - successCount;
                
                logger.LogInfo($"Migration Summary:");
                logger.LogInfo($"  Total: {results.Count}");
                logger.LogInfo($"  Successful: {successCount}");
                logger.LogInfo($"  Failed: {failureCount}");
                
                if (failureCount > 0)
                {
                    logger.LogError("Some migrations failed:");
                    foreach (var failedResult in results.Where(r => !r.Success))
                    {
                        logger.LogError($"  - {failedResult.RepositoryName}: {failedResult.ErrorMessage}");
                    }
                }
                
                return failureCount > 0 ? 1 : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL ERROR] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - Application failed: {ex}");
                return 1;
            }
        }
        
        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Register services using Dependency Injection
                    services.AddSingleton<ILoggerService, ConsoleLoggerService>();
                    services.AddSingleton<IFileSystemService, FileSystemService>();
                    services.AddSingleton<ICredentialService, CredentialService>();
                    services.AddSingleton<IConfigurationService, ConfigurationService>();
                    services.AddSingleton<IRepositoryService, RepositoryService>();
                    services.AddSingleton<IMigrationService, MigrationService>();
                });
    }
