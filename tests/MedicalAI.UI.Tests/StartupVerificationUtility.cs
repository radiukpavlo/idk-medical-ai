using System;
using System.Threading.Tasks;
using MedicalAI.UI;
using Microsoft.Extensions.DependencyInjection;
using MedicalAI.Core.Imaging;
using MedicalAI.Core.ML;

namespace MedicalAI.UI.Tests
{
    /// <summary>
    /// Utility class to verify application startup and initialization
    /// </summary>
    public static class StartupVerificationUtility
    {
        /// <summary>
        /// Verifies that the application can initialize successfully
        /// </summary>
        public static StartupVerificationResult VerifyApplicationStartup()
        {
            var result = new StartupVerificationResult();
            
            try
            {
                // Test 1: App creation
                result.AppCreationSuccess = TestAppCreation();
                
                // Test 2: App initialization
                result.AppInitializationSuccess = TestAppInitialization();
                
                // Test 3: Dependency injection setup
                result.DependencyInjectionSuccess = TestDependencyInjection();
                
                // Test 4: Main window creation
                result.MainWindowCreationSuccess = TestMainWindowCreation();
                
                // Test 5: Service resolution
                result.ServiceResolutionSuccess = TestServiceResolution();
                
                result.OverallSuccess = result.AppCreationSuccess && 
                                      result.AppInitializationSuccess && 
                                      result.DependencyInjectionSuccess && 
                                      result.MainWindowCreationSuccess && 
                                      result.ServiceResolutionSuccess;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.OverallSuccess = false;
            }
            
            return result;
        }
        
        private static bool TestAppCreation()
        {
            try
            {
                var app = new App();
                return app != null;
            }
            catch
            {
                return false;
            }
        }
        
        private static bool TestAppInitialization()
        {
            try
            {
                var app = new App();
                app.Initialize();
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        private static bool TestDependencyInjection()
        {
            try
            {
                var app = new App();
                app.Initialize();
                app.OnFrameworkInitializationCompleted();
                return App.Services != null;
            }
            catch
            {
                return false;
            }
        }
        
        private static bool TestMainWindowCreation()
        {
            try
            {
                var mainWindow = new MainWindow();
                return mainWindow != null && 
                       mainWindow.Title == "MedicalAI Thesis Suite" &&
                       mainWindow.Width == 1100 &&
                       mainWindow.Height == 720;
            }
            catch
            {
                return false;
            }
        }
        
        private static bool TestServiceResolution()
        {
            try
            {
                var app = new App();
                app.Initialize();
                app.OnFrameworkInitializationCompleted();
                
                // Test key service registrations
                var dicomService = App.Services.GetService<IDicomImportService>();
                var segmentationEngine = App.Services.GetService<ISegmentationEngine>();
                var classificationEngine = App.Services.GetService<IClassificationEngine>();
                var nlpService = App.Services.GetService<INlpReasoningService>();
                
                return dicomService != null && 
                       segmentationEngine != null && 
                       classificationEngine != null && 
                       nlpService != null;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Result of startup verification tests
    /// </summary>
    public class StartupVerificationResult
    {
        public bool OverallSuccess { get; set; }
        public bool AppCreationSuccess { get; set; }
        public bool AppInitializationSuccess { get; set; }
        public bool DependencyInjectionSuccess { get; set; }
        public bool MainWindowCreationSuccess { get; set; }
        public bool ServiceResolutionSuccess { get; set; }
        public Exception? Exception { get; set; }
        
        public void PrintResults()
        {
            Console.WriteLine("=== Application Startup Verification Results ===");
            Console.WriteLine($"Overall Success: {OverallSuccess}");
            Console.WriteLine($"App Creation: {AppCreationSuccess}");
            Console.WriteLine($"App Initialization: {AppInitializationSuccess}");
            Console.WriteLine($"Dependency Injection: {DependencyInjectionSuccess}");
            Console.WriteLine($"Main Window Creation: {MainWindowCreationSuccess}");
            Console.WriteLine($"Service Resolution: {ServiceResolutionSuccess}");
            
            if (Exception != null)
            {
                Console.WriteLine($"Exception: {Exception.Message}");
                Console.WriteLine($"Stack Trace: {Exception.StackTrace}");
            }
        }
    }
}