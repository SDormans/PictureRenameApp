namespace PictureRenameApp.Services
{
    /// <summary>
    /// Interface for application-wide logging abstraction.
    /// Provides methods to log messages at different severity levels.
    /// </summary>
    public interface IApplicationLogger
    {
        /// <summary>
        /// Logs an informational message.
        /// </summary>
        void LogInfo(string message);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        void LogWarning(string message);

        /// <summary>
        /// Logs an error message with optional exception details.
        /// </summary>
        void LogError(string message, Exception? exception = null);

        /// <summary>
        /// Logs a debug message (only in debug builds).
        /// </summary>
        void LogDebug(string message);
    }
}
