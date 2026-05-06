using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PictureRenameApp.Utilities
{
    /// <summary>
    /// Provides shared utility operations for file, directory, and image handling with error handling and rate limiting.
    /// </summary>
    public static class SharedOperations
    {
        // Simple rate limiter: max N operations per interval (per process)
        private static readonly SemaphoreSlim _rateLimiter = new(4); // e.g., 4 concurrent ops
        private static readonly TimeSpan _rateLimitDelay = TimeSpan.FromMilliseconds(100);

        public static async Task<TResult?> RateLimitedAsync<TResult>(Func<Task<TResult>> operation)
        {
            await _rateLimiter.WaitAsync();
            try
            {
                return await operation();
            }
            finally
            {
                // Release after a small delay to smooth bursty calls
                _ = Task.Delay(_rateLimitDelay).ContinueWith(_ => _rateLimiter.Release());
            }
        }

        public static async Task RateLimitedAsync(Func<Task> operation)
        {
            await _rateLimiter.WaitAsync();
            try
            {
                await operation();
            }
            finally
            {
                _ = Task.Delay(_rateLimitDelay).ContinueWith(_ => _rateLimiter.Release());
            }
        }

        public static List<string> SafeGetFiles(string directoryPath, HashSet<string> allowedExtensions, Action<string>? logWarning = null)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            {
                logWarning?.Invoke($"Directory does not exist or path is empty: {directoryPath}");
                return result;
            }
            try
            {
                foreach (var file in Directory.GetFiles(directoryPath))
                {
                    if (allowedExtensions.Contains(Path.GetExtension(file)))
                        result.Add(file);
                }
            }
            catch (Exception ex)
            {
                logWarning?.Invoke($"Error reading directory {directoryPath}: {ex.Message}");
            }
            return result;
        }

        public static bool SafeFileExists(string path)
        {
            try { return File.Exists(path); } catch { return false; }
        }

        public static bool SafeDirectoryExists(string path)
        {
            try { return Directory.Exists(path); } catch { return false; }
        }

        public static string SafeFormatFileSize(long bytes)
        {
            if (bytes < 0) return "0 B";
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public static Image? SafeLoadImage(string filePath, Action<string>? logWarning = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    logWarning?.Invoke($"Image load failed: file not found or invalid path: {filePath}");
                    return null;
                }
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var image = Image.FromStream(fs, false, false);
                return new Bitmap(image);
            }
            catch (Exception ex)
            {
                logWarning?.Invoke($"Failed to load image: {ex.Message}");
                return null;
            }
        }
    }
}
