using Microsoft.Extensions.DependencyInjection;
using PictureRenameApp.Services;

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
            var services = new ServiceCollection();

            // Register core services with their implementations
            services.AddSingleton<IApplicationLogger, ApplicationLogger>();
            services.AddSingleton<IImageService, ImageService>();
            services.AddSingleton<IFileService, FileService>();

            return services.BuildServiceProvider();
        }
    }
}
