using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MedicalAI.Core.Imaging
{
    public record DicomImportOptions(bool Anonymize = true);
    public record ImportResult(int StudiesImported, int SeriesImported, int ImagesImported);

    public interface IDicomImportService
    {
        Task<ImportResult> ImportAsync(string path, DicomImportOptions options, CancellationToken ct);
    }

    public record AnonymizerProfile(string Name);

    public interface IDicomAnonymizerService
    {
        Task<int> AnonymizeInPlaceAsync(IEnumerable<string> filePaths, AnonymizerProfile profile, CancellationToken ct);
    }

    public record ImageRef(string Modality, string FilePath, string? SeriesInstanceUid, int? InstanceNumber);

    public interface IVolumeStore
    {
        Task<Volume3D> LoadAsync(ImageRef imageRef, CancellationToken ct);
        Task SaveMaskAsync(ImageRef imageRef, Mask3D mask, CancellationToken ct);
    }
}
namespace MedicalAI.Core.ML
{
    using MedicalAI.Core.Imaging;
    using System.Threading;
    using System.Threading.Tasks;

    public record SegmentationOptions(string ModelPath, float Threshold = 0.5f);
    public record SegmentationResult(Mask3D Mask, Dictionary<int,string> Labels);
    public interface ISegmentationEngine
    {
        Task<SegmentationResult> RunAsync(Volume3D volume, SegmentationOptions options, CancellationToken ct);
    }

    public record ClassificationResult(Dictionary<string, float> Probabilities, Dictionary<int, float>? NodeImportances = null);
    public interface IClassificationEngine
    {
        Task<ClassificationResult> PredictAsync(GraphDescriptor graph, CancellationToken ct);
    }

    public record DistillationConfig(string DatasetPath, string[] TeacherModelPaths, string StudentModelPath);
    public record DistillationRunId(string Value);
    public enum RunState { Pending, Running, Completed, Failed }
    public record RunStatus(RunState State, string? Message = null);
    public record ModelInfo(string Path, string Sha256, string? Notes);

    public interface IKnowledgeDistillationService
    {
        Task<DistillationRunId> StartAsync(DistillationConfig config, CancellationToken ct);
        Task<RunStatus> GetStatusAsync(DistillationRunId id, CancellationToken ct);
        Task<ModelInfo> GetStudentModelAsync(DistillationRunId id, CancellationToken ct);
    }

    public enum Sentiment { Negative, Neutral, Positive }
    public record CaseContext(string PatientPseudoId, string StudyId, string? Notes, SegmentationResult? Segmentation, ClassificationResult? Classification);
    public record NlpSummary(string Text, Sentiment Sentiment);

    public interface INlpReasoningService
    {
        Task<NlpSummary> SummarizeAsync(CaseContext context, CancellationToken ct);
        Task<Sentiment> AnalyzeSentimentAsync(string text, CancellationToken ct);
    }
}
namespace MedicalAI.Core
{
    using System;
    using System.Collections.Generic;

    public record Patient(Guid Id, string PseudoId);
    public record Study(Guid Id, Guid PatientId, string StudyInstanceUid, DateTime CreatedUtc);
    public record Series(Guid Id, Guid StudyId, string SeriesInstanceUid, string Modality, string? Path);
    public record Volume3D(int Width, int Height, int Depth, float VoxX, float VoxY, float VoxZ, byte[] Voxels);
    public record Mask3D(int Width, int Height, int Depth, byte[] Labels);

    public record GraphNode(int Id, float[] Features);
    public record GraphEdge(int Source, int Target);
    public record GraphDescriptor(List<GraphNode> Nodes, List<GraphEdge> Edges);
}
