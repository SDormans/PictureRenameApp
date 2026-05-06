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

                // Configure services first (without form context)
                var serviceProvider = ServiceConfiguration.ConfigureServices(uiControl: null);

                // Resolve services
                var logger = serviceProvider.GetRequiredService<Services.IApplicationLogger>();
                var controller = serviceProvider.GetRequiredService<Controllers.IApplicationController>();
                
                logger.LogInfo("=== Application Starting ===");

                try
                {
                    // Create form with proper dependencies
                    var form = new Form1(logger, controller);
                    logger.LogDebug("Form1 created successfully");

                    Application.Run(form);

                    logger.LogInfo("=== Application Ended ===");
                }
                catch (ArgumentException gdiEx) when (gdiEx.Message.Contains("Parameter"))
                {
                    // Specific handling for GDI+ "Parameter is not valid" exception
                    logger.LogError("GDI+ error during form initialization or rendering. This may indicate corrupted image data or invalid image dimensions.", gdiEx);
                    
                    MessageBox.Show(
                        $"A graphics error occurred during startup: {gdiEx.Message}\n\n" +
                        $"This may be due to corrupted image files or invalid image data.\n" +
                        $"Please check the logs in the Logs folder for details.",
                        "Graphics Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                catch (Exception formEx)
                {
                    logger.LogError("Error during form execution", formEx);
                    
                    MessageBox.Show(
                        $"An error occurred during form execution: {formEx.Message}\n\nPlease check the logs in the Logs folder.",
                        "Form Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
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
