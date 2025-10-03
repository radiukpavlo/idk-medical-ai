using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MedicalAI.Core;
using MedicalAI.Core.ML;
using MedicalAI.Core.Performance;
using MedicalAI.Infrastructure.ML;

namespace Segmentation.SKIFSeg
{
    public class SkifSegEngine : ISegmentationEngine
    {
        private readonly MockSegmentationEngine _fallback;

        public SkifSegEngine(ILogger<MockSegmentationEngine> logger, IParallelProcessor parallelProcessor, IMemoryManager memoryManager)
        {
            _fallback = new MockSegmentationEngine(logger, parallelProcessor, memoryManager);
        }

        public Task<SegmentationResult> RunAsync(Volume3D volume, SegmentationOptions options, CancellationToken ct)
            => _fallback.RunAsync(volume, options, ct); // placeholder ONNX runner can be added
    }

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSkifSeg(this IServiceCollection s)
        {
            s.AddSingleton<ISegmentationEngine, SkifSegEngine>();
            return s;
        }
    }
}
