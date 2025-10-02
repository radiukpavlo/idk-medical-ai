using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MedicalAI.Infrastructure.Db;
using MedicalAI.Infrastructure.Imaging;
using MedicalAI.Infrastructure.ML;
using MedicalAI.Infrastructure.Performance;
using MedicalAI.Infrastructure.Security;
using MedicalAI.Infrastructure.Diagnostics;
using MedicalAI.Core.Imaging;
using MedicalAI.Core.ML;
using MedicalAI.Core.Performance;
using MedicalAI.Core.Security;
using MedicalAI.Core.Diagnostics;

namespace MedicalAI.Infrastructure.DI
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? sqlitePath = null)
        {
            services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={sqlitePath ?? "app.db"}"));
            
            // Performance services
            services.AddSingleton<IMemoryManager, MemoryManager>();
            services.AddSingleton<ILargeImageManager, LargeImageManager>();
            services.AddSingleton<IParallelProcessor, ParallelProcessor>();
            services.AddSingleton<IBackgroundTaskManager, BackgroundTaskManager>();
            
            // Imaging services
            services.AddScoped<IDicomImportService, DicomImportService>();
            services.AddScoped<IDicomAnonymizerService, DicomAnonymizerService>();
            services.AddScoped<IVolumeStore, VolumeStore>();
            
            // ML services
            services.AddScoped<ISegmentationEngine, MockSegmentationEngine>();
            services.AddScoped<IClassificationEngine, MockClassificationEngine>();
            services.AddSingleton<IKnowledgeDistillationService, InMemoryDistillationService>();
            services.AddSingleton<INlpReasoningService, SimpleNlpUAService>();
            
            // Security services
            services.AddSingleton<ISecurityService, SecurityService>();
            services.AddScoped<IEnhancedDicomAnonymizer, EnhancedDicomAnonymizer>();
            services.AddSingleton<IAuditLogger, AuditLogger>();
            
            // Diagnostic services
            services.AddSingleton<IErrorHandler, ErrorHandler>();
            services.AddSingleton<IDiagnosticService, DiagnosticService>();
            services.AddSingleton<IStructuredLoggingService, StructuredLoggingService>();
            
            return services;
        }
    }
}
