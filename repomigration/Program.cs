using System;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RepositoryMigration.Interfaces;
using RepositoryMigration.Services;

namespace RepositoryMigration
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Display version information
            DisplayVersionInfo();

            // Handle version argument
            if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
            {
                return;
            }

            var configPath = GetConfigPath(args);
            if (string.IsNullOrEmpty(configPath))
            {
                ShowUsage();
                return;
            }

            if (!File.Exists(configPath))
            {
                Console.WriteLine($"Error: Configuration file not found: {configPath}");
                return;
            }

            var host = CreateHostBuilder(args).Build();
            
            try
            {
                Console.WriteLine($"Starting migration using config: {configPath}");
                var migrator = host.Services.GetRequiredService<IRepositoryMigrator>();
                await migrator.MigrateAllRepositoriesAsync(configPath);
                Console.WriteLine("Migration completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration failed: {ex.Message}");
                Environment.Exit(1);
            }
        }

        private static void DisplayVersionInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                         ?? assembly.GetName().Version?.ToString()
                         ?? "Unknown";
            
            var product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "Repository Migration Tool";
            var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";
            
            Console.WriteLine($"{product} v{version}");
            if (!string.IsNullOrEmpty(copyright))
            {
                Console.WriteLine(copyright);
            }
            Console.WriteLine();
        }
        private static string GetConfigPath(string[] args)
        {
            if (args.Length > 0)
                return args[0];

            // Default config file locations to check
            var defaultPaths = new[]
            {
                "configs/migration-config.yaml",
                "migration-config.yaml",
                "../configs/migration-config.yaml"
            };

            foreach (var path in defaultPaths)
            {
                if (File.Exists(path))
                {
                    Console.WriteLine($"Using default config file: {path}");
                    return path;
                }
            }

            return string.Empty;
        }

        private static void ShowUsage()
        {
            Console.WriteLine("Repository Migration Tool");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  RepositoryMigration <config-file>");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  RepositoryMigration configs/migration-config.yaml");
            Console.WriteLine("  RepositoryMigration /path/to/production-config.yaml");
            Console.WriteLine();
            Console.WriteLine("If no config file is specified, the tool will look for:");
            Console.WriteLine("  - configs/migration-config.yaml");
            Console.WriteLine("  - migration-config.yaml");
            Console.WriteLine("  - ../configs/migration-config.yaml");
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddLogging(builder => builder.AddConsole());
                    services.AddScoped<IGitCommandExecutor, GitCommandExecutor>();
                    services.AddScoped<IRepositoryMigrator, RepositoryMigrator>();
                    services.AddScoped<IYamlConfigReader, YamlConfigReader>();
                });
    }
}