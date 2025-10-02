using System.Threading;
using System.Threading.Tasks;
using MedicalAI.Core.ML;
using MedicalAI.Infrastructure.ML;

namespace Distillation.MultiTeacher
{
    public class DistillationService : IKnowledgeDistillationService
    {
        private readonly InMemoryDistillationService _inner = new();
        public Task<DistillationRunId> StartAsync(DistillationConfig config, CancellationToken ct) => _inner.StartAsync(config, ct);
        public Task<RunStatus> GetStatusAsync(DistillationRunId id, CancellationToken ct) => _inner.GetStatusAsync(id, ct);
        public Task<ModelInfo> GetStudentModelAsync(DistillationRunId id, CancellationToken ct) => _inner.GetStudentModelAsync(id, ct);
    }
}
