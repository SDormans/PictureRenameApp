using System.Diagnostics;

namespace PictureRenameApp.Services
{
    /// <summary>
    /// Concrete implementation of IApplicationLogger.
    /// Provides logging functionality with thread-safe operations and error handling.
    /// </summary>
    public class ApplicationLogger : IApplicationLogger
    {
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Initializes a new instance of ApplicationLogger.
        /// Creates a log file in the application directory if it doesn't exist.
        /// </summary>
        public ApplicationLogger()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var logDir = Path.Combine(appDir, "Logs");

            try
            {
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                _logFilePath = Path.Combine(logDir, $"app_{DateTime.Now:yyyy-MM-dd}.log");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize logging: {ex.Message}");
                _logFilePath = string.Empty;
            }
        }

        public void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        public void LogWarning(string message)
        {
            WriteLog("WARN", message);
        }

        public void LogError(string message, Exception? exception = null)
        {
            var exceptionDetails = exception != null ? $"\n{exception}" : string.Empty;
            WriteLog("ERROR", $"{message}{exceptionDetails}");
        }

        public void LogDebug(string message)
        {
#if DEBUG
            WriteLog("DEBUG", message);
#endif
        }

        /// <summary>
        /// Internal method to write log entries to file and debug output.
        /// Thread-safe with lock to prevent concurrent write issues.
        /// </summary>
        private void WriteLog(string level, string message)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

            // Always write to debug output
            Debug.WriteLine(logEntry);

            // Write to file if path is valid
            if (!string.IsNullOrEmpty(_logFilePath))
            {
                lock (_lockObject)
                {
                    try
                    {
                        File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to write to log file: {ex.Message}");
                    }
                }
            }
        }
    }
}
