using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MedicalAI.Core;
using MedicalAI.Core.ML;

namespace MedicalAI.Infrastructure.ML
{
    public class MockSegmentationEngine : ISegmentationEngine
    {
        public Task<SegmentationResult> RunAsync(Volume3D volume, SegmentationOptions options, CancellationToken ct)
        {
            // Simple threshold-based 3D mask for demo; deterministic
            byte[] mask = new byte[volume.Voxels.Length];
            for (int i=0;i<volume.Voxels.Length;i++)
                mask[i] = volume.Voxels[i] >= (byte)(options.Threshold*255f) ? (byte)1 : (byte)0;
            var labels = new Dictionary<int, string>{{1,"Myocardium"}};
            return Task.FromResult(new SegmentationResult(new Mask3D(volume.Width, volume.Height, volume.Depth, mask), labels));
        }
    }

    public class MockClassificationEngine : IClassificationEngine
    {
        public Task<ClassificationResult> PredictAsync(GraphDescriptor graph, CancellationToken ct)
        {
            // Deterministic pseudo-probabilities from features sum
            double sum = graph.Nodes.SelectMany(n => n.Features).Select(f => (double)f).Sum();
            float p1 = (float)((Math.Sin(sum)+1)/2.0);
            float p0 = 1 - p1;
            var dict = new Dictionary<string,float>{{"Normal", p0}, {"Pathology", p1}};
            return Task.FromResult(new ClassificationResult(dict));
        }
    }

    public class InMemoryDistillationService : IKnowledgeDistillationService
    {
        private readonly Dictionary<string, RunStatus> _status = new();
        public Task<DistillationRunId> StartAsync(DistillationConfig config, CancellationToken ct)
        {
            var id = new DistillationRunId(Guid.NewGuid().ToString());
            _status[id.Value] = new RunStatus(RunState.Running, "Mock training started");
            // Immediately complete for demo
            _status[id.Value] = new RunStatus(RunState.Completed, "Mock training completed");
            return Task.FromResult(id);
        }
        public Task<RunStatus> GetStatusAsync(DistillationRunId id, CancellationToken ct)
            => Task.FromResult(_status.TryGetValue(id.Value, out var s) ? s : new RunStatus(RunState.Failed, "NotFound"));
        public Task<ModelInfo> GetStudentModelAsync(DistillationRunId id, CancellationToken ct)
        {
            // Create a fake SHA for demo
            var sha = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(id.Value)));
            return Task.FromResult(new ModelInfo("models/student.onnx", sha, "Mock model"));
        }
    }

    public class SimpleNlpUAService : INlpReasoningService
    {
        public Task<NlpSummary> SummarizeAsync(CaseContext ctx, CancellationToken ct)
        {
            // Ukrainian template referencing segmentation & classification
            var seg = ctx.Segmentation != null ? "Виконано сегментацію міокарда; показники доступні." : "Сегментацію не виконано.";
            var cls = ctx.Classification != null ? $"Класифікація: {TopClass(ctx.Classification)}." : "Класифікація відсутня.";
            var baseText = $"Пацієнт {ctx.PatientPseudoId}. Дослідження {ctx.StudyId}. {seg} {cls} Узагальнення сформовано автоматично для дослідницьких цілей.";
            var sent = Analyze(baseText);
            return Task.FromResult(new NlpSummary(baseText, sent));
        }

        public Task<Sentiment> AnalyzeSentimentAsync(string text, CancellationToken ct)
            => Task.FromResult(Analyze(text));

        private static Sentiment Analyze(string text)
        {
            string lower = text.ToLowerInvariant();
            if (lower.Contains("патолог") || lower.Contains("негатив")) return Sentiment.Negative;
            if (lower.Contains("норма") || lower.Contains("без ознак")) return Sentiment.Positive;
            return Sentiment.Neutral;
        }

        private static string TopClass(ClassificationResult r)
            => r.Probabilities.OrderByDescending(kv => kv.Value).First().Key;
    }
}
