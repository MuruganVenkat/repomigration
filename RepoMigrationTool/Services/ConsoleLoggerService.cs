using RepoMigrationTool.Interfaces;
using RepoMigrationTool.Models;

namespace RepoMigrationTool.Services;

public class ConsoleLoggerService : ILoggerService
    {
        public void LogInfo(string message)
        {
            Console.WriteLine($"[INFO] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        public void LogError(string message, Exception? exception = null)
        {
            Console.WriteLine($"[ERROR] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {message}");
            if (exception != null)
            {
                Console.WriteLine($"Exception: {exception}");
            }
        }

        public void LogWarning(string message)
        {
            Console.WriteLine($"[WARNING] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {message}");
        }
    }