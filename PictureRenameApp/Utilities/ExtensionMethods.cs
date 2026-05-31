using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using PictureRenameApp.Configuration;

namespace PictureRenameApp.Utilities
{
    /// <summary>
    /// Extension methods for common operations used throughout the application.
    /// Centralizes utility functions to reduce code duplication.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Safely attempts to get the file extension, handling null or invalid paths.
        /// </summary>
        public static string SafeGetExtension(this string filePath)
        {
            try
            {
                return string.IsNullOrWhiteSpace(filePath) ? string.Empty : Path.GetExtension(filePath);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Safely attempts to get the file name, handling null or invalid paths.
        /// </summary>
        public static string SafeGetFileName(this string filePath)
        {
            try
            {
                return string.IsNullOrWhiteSpace(filePath) ? string.Empty : Path.GetFileName(filePath);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Safely attempts to get the directory name, handling null or invalid paths.
        /// </summary>
        public static string SafeGetDirectoryName(this string filePath)
        {
            try
            {
                return string.IsNullOrWhiteSpace(filePath) ? string.Empty : Path.GetDirectoryName(filePath) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Determines if a file path has a supported image extension.
        /// </summary>
        public static bool IsSupportedImage(this string filePath)
        {
            var ext = filePath.SafeGetExtension();
            return AppConstants.SupportedImageExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines if a file exists, returning false if any exception occurs.
        /// </summary>
        public static bool SafeExists(this FileInfo file)
        {
            try
            {
                return file?.Exists ?? false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Formats bytes into human-readable file size string.
        /// </summary>
        public static string FormatFileSize(this long bytes)
        {
            if (bytes < 0) return "0 B";

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return len % 1 == 0 
                ? $"{len:0} {sizes[order]}" 
                : $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Safely disposes of an image resource, handling null or already-disposed cases.
        /// </summary>
        public static void SafeDispose(this Image? image)
        {
            try
            {
                image?.Dispose();
            }
            catch
            {
                // Silently ignore disposal errors
            }
        }

        /// <summary>
        /// Returns the number of items in a collection safely, returning 0 for null collections.
        /// </summary>
        public static int SafeCount<T>(this IEnumerable<T>? collection)
        {
            try
            {
                return collection?.Count() ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Safely checks if a string matches any value in a case-insensitive collection.
        /// </summary>
        public static bool EqualsAnyIgnoreCase(this string? value, params string[] compareTo)
        {
            if (string.IsNullOrEmpty(value)) return false;
            return compareTo.Any(c => string.Equals(value, c, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Safely trims a string, handling null values.
        /// </summary>
        public static string SafeTrim(this string? value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        }

        /// <summary>
        /// Safely checks if a directory exists, handling null or invalid paths.
        /// </summary>
        public static bool SafeDirectoryExists(this string? path)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Safely checks if a file exists, handling null or invalid paths.
        /// </summary>
        public static bool SafeFileExists(this string? path)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Safely enumerates files in a directory, handling access exceptions.
        /// </summary>
        public static IEnumerable<FileInfo> SafeEnumerateFiles(this DirectoryInfo directory)
        {
            try
            {
                return directory?.EnumerateFiles() ?? Enumerable.Empty<FileInfo>();
            }
            catch
            {
                return Enumerable.Empty<FileInfo>();
            }
        }

        /// <summary>
        /// Creates a shallow copy of a bitmap.
        /// </summary>
        public static Bitmap? CloneBitmap(this Image? image)
        {
            try
            {
                if (image == null) return null;
                return new Bitmap(image);
            }
            catch
            {
                return null;
            }
        }
    }
}
