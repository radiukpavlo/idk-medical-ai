using System.Threading;
using System.Threading.Tasks;
using MedicalAI.Core.ML;
using MedicalAI.Infrastructure.ML;

namespace Classification.KIGCN
{
    public class KigcnEngine : IClassificationEngine
    {
        private readonly MockClassificationEngine _fallback = new();
        public Task<ClassificationResult> PredictAsync(MedicalAI.Core.GraphDescriptor graph, CancellationToken ct)
            => _fallback.PredictAsync(graph, ct);
    }
}
