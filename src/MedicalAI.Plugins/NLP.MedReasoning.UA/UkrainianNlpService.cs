using System.Threading;
using System.Threading.Tasks;
using MedicalAI.Core.ML;
using MedicalAI.Infrastructure.ML;

namespace NLP.MedReasoning.UA
{
    public class UkrainianNlpService : INlpReasoningService
    {
        private readonly SimpleNlpUAService _impl = new();
        public Task<NlpSummary> SummarizeAsync(CaseContext context, CancellationToken ct) => _impl.SummarizeAsync(context, ct);
        public Task<Sentiment> AnalyzeSentimentAsync(string text, CancellationToken ct) => _impl.AnalyzeSentimentAsync(text, ct);
    }
}
