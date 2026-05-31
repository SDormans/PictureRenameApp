using Microsoft.Extensions.DependencyInjection;
using PictureRenameApp.Configuration;

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

                var logger = serviceProvider.GetRequiredService<Services.IApplicationLogger>();
                _ = serviceProvider.GetRequiredService<Controllers.IApplicationController>();
                
                logger.LogInfo("=== Application Starting ===");

                try
                {
                    // Main UI (carousel-style shell); services remain registered for future wiring.
                    var form = new MainForm();
                    logger.LogDebug("MainForm created successfully");

                    Application.Run(form);

                    logger.LogInfo("=== Application Ended ===");
                }
                catch (ArgumentException gdiEx) when (gdiEx.Message.Contains("Parameter"))
                {
                    // Specific handling for GDI+ "Parameter is not valid" exception
                    logger.LogError("GDI+ error during form initialization or rendering. This may indicate corrupted image data or invalid image dimensions.", gdiEx);
                    
                    MessageBox.Show(
                        string.Format(AppConstants.GraphicsErrorMessage, gdiEx.Message),
                        AppConstants.GraphicsErrorTitle,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                catch (Exception formEx)
                {
                    logger.LogError("Critical error during application startup", formEx);
                    MessageBox.Show(
                        $"An unexpected error occurred during startup:\n{formEx.Message}",
                        AppConstants.ErrorTitle,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize application:\n{ex.Message}",
                    AppConstants.ErrorTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
