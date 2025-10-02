using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MedicalAI.Core;
using MedicalAI.Core.ML;

namespace MedicalAI.Application.Commands
{
    public record RunClassificationCommand(GraphDescriptor Graph) : IRequest<ClassificationResult>;

    public class RunClassificationHandler : IRequestHandler<RunClassificationCommand, ClassificationResult>
    {
        private readonly IClassificationEngine _engine;
        public RunClassificationHandler(IClassificationEngine engine){ _engine = engine; }
        public Task<ClassificationResult> Handle(RunClassificationCommand request, CancellationToken ct)
            => _engine.PredictAsync(request.Graph, ct);
    }
}
