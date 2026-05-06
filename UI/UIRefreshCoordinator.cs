using System;
using System.Windows.Forms;
using PictureRenameApp.Controllers;
using PictureRenameApp.Models;

namespace PictureRenameApp.UI
{
    /// <summary>
    /// Manages UI refresh synchronization and thread safety.
    /// Single Responsibility: Coordinate UI updates without race conditions.
    /// </summary>
    public interface IUIRefreshCoordinator
    {
        /// <summary>
        /// Pauses collection change notifications during bulk operations.
        /// </summary>
        void Pause();

        /// <summary>
        /// Resumes collection change notifications.
        /// </summary>
        void Resume();

        /// <summary>
        /// Checks if refresh is currently in progress.
        /// </summary>
        bool IsRefreshing { get; }

        /// <summary>
        /// Subscribes to collection changes for UI updates.
        /// </summary>
        void Subscribe(Action refreshAction);
    }

    /// <summary>
    /// Implementation of UI refresh coordination.
    /// </summary>
    public class UIRefreshCoordinator : IUIRefreshCoordinator
    {
        private bool _isRefreshing = false;
        private bool _isPaused = false;
        private Action? _refreshAction;

        public bool IsRefreshing => _isRefreshing;

        /// <inheritdoc/>
        public void Pause()
        {
            _isPaused = true;
        }

        /// <inheritdoc/>
        public void Resume()
        {
            _isPaused = false;
        }

        /// <inheritdoc/>
        public void Subscribe(Action refreshAction)
        {
            _refreshAction = refreshAction ?? throw new ArgumentNullException(nameof(refreshAction));
        }

        /// <summary>
        /// Triggers a refresh if not paused or already refreshing.
        /// </summary>
        public void TriggerRefresh()
        {
            if (_isPaused || _isRefreshing)
                return;

            _isRefreshing = true;
            try
            {
                _refreshAction?.Invoke();
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        /// <summary>
        /// Executes an action while suppressing refresh notifications.
        /// </summary>
        public void ExecuteWithoutRefresh(Action action)
        {
            Pause();
            try
            {
                action();
            }
            finally
            {
                Resume();
            }
        }
    }
}
