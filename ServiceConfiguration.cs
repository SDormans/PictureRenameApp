using Microsoft.Extensions.DependencyInjection;
using System.Windows.Forms;
using PictureRenameApp.Services;
using PictureRenameApp.Models;
using PictureRenameApp.Controllers;

namespace PictureRenameApp
{
    /// <summary>
    /// Service collection configuration for dependency injection setup.
    /// Centralizes the registration of all services used throughout the application.
    /// </summary>
    public static class ServiceConfiguration
    {
        /// <summary>
        /// Configures and registers all application services.
        /// </summary>
        /// <returns>Configured IServiceProvider ready for dependency injection</returns>
        public static IServiceProvider ConfigureServices()
        {
            return ConfigureServices(uiControl: null);
        }

        /// <summary>
        /// Configures and registers all application services with optional UI control for thread synchronization.
        /// </summary>
        /// <param name="uiControl">Optional UI control for thread synchronization (e.g., Form1)</param>
        /// <returns>Configured IServiceProvider ready for dependency injection</returns>
        public static IServiceProvider ConfigureServices(Control? uiControl = null)
        {
            var services = new ServiceCollection();

            // Register core services with their implementations
            services.AddSingleton<IApplicationLogger, ApplicationLogger>();
            services.AddSingleton<IImageService, ImageService>();
            services.AddSingleton<IFileService, FileService>();

            // Register MVC pattern components
            services.AddSingleton<IApplicationModel, ApplicationModel>();
            
            // Register controller with UI control for thread-safe model updates
            services.AddSingleton<IApplicationController>(provider =>
                new ApplicationController(
                    provider.GetRequiredService<IApplicationLogger>(),
                    provider.GetRequiredService<IImageService>(),
                    provider.GetRequiredService<IFileService>(),
                    provider.GetRequiredService<IApplicationModel>(),
                    uiControl));

            return services.BuildServiceProvider();
        }
    }
}
