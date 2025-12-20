using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuxiliumMicroservices.Common.Utilities
{
    internal static class Logger
    {
        private static readonly object _lockObj = new object();
        private static string _logFilePath;

        static Logger()
        {
            // Create logs directory if it doesn't exist
            string logDirectory = ConfigurationUtilities.GetString("Logging", "LogStorageDirectory");

            Directory.CreateDirectory(logDirectory);

            // Create log file with timestamp
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _logFilePath = Path.Combine(logDirectory, $"auxilium_{timestamp}.log");
        }

        internal static void Log(string level, string queueName, string message)
        {
            string logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [{queueName}] {message}";

            lock (_lockObj)
            {
                try
                {
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    // Failsafe: if file logging fails, at least try to output somewhere
                    Console.Error.WriteLine($"Failed to write to log file: {ex.Message}");
                }
            }
        }

        internal static void Info(string queueName, string message) => Log("INFO", queueName, message);
        internal static void Error(string queueName, string message) => Log("ERROR", queueName, message);
        internal static void Warning(string queueName, string message) => Log("WARN", queueName, message);
        internal static void Debug(string queueName, string message) => Log("DEBUG", queueName, message);

        internal static string GetLogFilePath() => _logFilePath;
    }
}