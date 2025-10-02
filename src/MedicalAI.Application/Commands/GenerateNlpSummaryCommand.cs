using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MedicalAI.Core.ML;

namespace MedicalAI.Application.Commands
{
    public record GenerateNlpSummaryCommand(CaseContext Context) : IRequest<NlpSummary>;

    public class GenerateNlpSummaryHandler : IRequestHandler<GenerateNlpSummaryCommand, NlpSummary>
    {
        private readonly INlpReasoningService _nlp;
        public GenerateNlpSummaryHandler(INlpReasoningService nlp){ _nlp = nlp; }
        public Task<NlpSummary> Handle(GenerateNlpSummaryCommand request, CancellationToken ct)
            => _nlp.SummarizeAsync(request.Context, ct);
    }
}
