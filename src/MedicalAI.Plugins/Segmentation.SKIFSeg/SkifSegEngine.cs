using System.Threading;
using System.Threading.Tasks;
using MedicalAI.Core;
using MedicalAI.Core.ML;
using MedicalAI.Infrastructure.ML;
using Microsoft.Extensions.DependencyInjection;

namespace Segmentation.SKIFSeg
{
    public class SkifSegEngine : ISegmentationEngine
    {
        private readonly MockSegmentationEngine _fallback = new();
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
