using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PictureRenameApp.Models;

namespace PictureRenameApp.Controllers
{
    /// <summary>
    /// Interface for the application controller.
    /// Defines the contract for business logic operations in the MVC pattern.
    /// </summary>
    public interface IApplicationController
    {
        /// <summary>
        /// Gets the application model.
        /// </summary>
        /// <returns>The application model instance</returns>
        IApplicationModel GetModel();

        /// <summary>
        /// Loads a directory of images asynchronously.
        /// </summary>
        /// <param name="directoryPath">The directory path to load</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task LoadDirectoryAsync(string directoryPath);

        /// <summary>
        /// Displays a file with preview and metadata asynchronously.
        /// </summary>
        /// <param name="filePath">The file path to display</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task DisplayFileAsync(string filePath);

        /// <summary>
        /// Renames a single file asynchronously.
        /// </summary>
        /// <param name="sourcePath">The source file path</param>
        /// <param name="newName">The new name (without extension)</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task RenameSingleFileAsync(string sourcePath, string newName);

        /// <summary>
        /// Renames multiple files in batch asynchronously.
        /// </summary>
        /// <param name="selectedFiles">List of file paths to rename</param>
        /// <param name="baseName">The base name for all files</param>
        /// <param name="startIndex">The starting index for numbered suffixes</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task RenameBatchFilesAsync(List<string> selectedFiles, string baseName, int startIndex);

        /// <summary>
        /// Clears all application state.
        /// </summary>
        void ClearAll();
    }
}
