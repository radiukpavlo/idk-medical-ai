using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MedicalAI.Infrastructure.Db;
using MedicalAI.Infrastructure.Imaging;
using MedicalAI.Infrastructure.ML;
using MedicalAI.Core.Imaging;
using MedicalAI.Core.ML;

namespace MedicalAI.Infrastructure.DI
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? sqlitePath = null)
        {
            services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={sqlitePath ?? "app.db"}"));
            services.AddSingleton<IDicomImportService, DicomImportService>();
            services.AddSingleton<IDicomAnonymizerService, DicomAnonymizerService>();
            services.AddSingleton<IVolumeStore, VolumeStore>();
            services.AddSingleton<ISegmentationEngine, MockSegmentationEngine>();
            services.AddSingleton<IClassificationEngine, MockClassificationEngine>();
            services.AddSingleton<IKnowledgeDistillationService, InMemoryDistillationService>();
            services.AddSingleton<INlpReasoningService, SimpleNlpUAService>();
            return services;
        }
    }
}
