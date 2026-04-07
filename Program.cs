using Microsoft.Extensions.DependencyInjection;

namespace PictureRenameApp
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        ///  Initializes dependency injection container and starts the main form.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                ApplicationConfiguration.Initialize();

                // Configure and build the service provider for dependency injection
                var serviceProvider = ServiceConfiguration.ConfigureServices();

                // Resolve the logger to log startup
                var logger = serviceProvider.GetRequiredService<Services.IApplicationLogger>();
                logger.LogInfo("=== Application Starting ===");

                // Resolve and run the main form with injected dependencies
                var form = new Form1(
                    serviceProvider.GetRequiredService<Services.IApplicationLogger>(),
                    serviceProvider.GetRequiredService<Services.IImageService>(),
                    serviceProvider.GetRequiredService<Services.IFileService>()
                );

                Application.Run(form);

                logger.LogInfo("=== Application Ended ===");
            }
            catch (Exception ex)
            {
                // Fallback error handling if DI setup fails
                MessageBox.Show(
                    $"Failed to start application: {ex.Message}\n\nPlease check the logs in the Logs folder.",
                    "Startup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                // Try to log the error if logger is available
                try
                {
                    var logger = ServiceConfiguration.ConfigureServices().GetRequiredService<Services.IApplicationLogger>();
                    logger.LogError("Application startup failed", ex);
                }
                catch
                {
                    // Silently fail if we can't even log
                }
            }
        }
    }
}