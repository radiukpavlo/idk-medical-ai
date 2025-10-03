using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MedicalAI.Core.ML;
using MedicalAI.Core.Performance;
using MedicalAI.Infrastructure.ML;

namespace Classification.KIGCN
{
    public class KigcnEngine : IClassificationEngine
    {
        private readonly MockClassificationEngine _fallback;

        public KigcnEngine(ILogger<MockClassificationEngine> logger, IParallelProcessor parallelProcessor)
        {
            _fallback = new MockClassificationEngine(logger, parallelProcessor);
        }

        public Task<ClassificationResult> PredictAsync(MedicalAI.Core.GraphDescriptor graph, CancellationToken ct)
            => _fallback.PredictAsync(graph, ct);
    }
}
