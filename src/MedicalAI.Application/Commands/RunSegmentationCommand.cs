using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MedicalAI.Core;
using MedicalAI.Core.ML;

namespace MedicalAI.Application.Commands
{
    public record RunSegmentationCommand(Volume3D Volume, string ModelPath, float Threshold) : IRequest<SegmentationResult>;

    public class RunSegmentationHandler : IRequestHandler<RunSegmentationCommand, SegmentationResult>
    {
        private readonly ISegmentationEngine _engine;
        public RunSegmentationHandler(ISegmentationEngine engine){ _engine = engine; }
        public Task<SegmentationResult> Handle(RunSegmentationCommand request, CancellationToken ct)
            => _engine.RunAsync(request.Volume, new SegmentationOptions(request.ModelPath, request.Threshold), ct);
    }
}
