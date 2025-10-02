# API Documentation

This document provides comprehensive API documentation for the MedicalAI Thesis Suite, including all interfaces, classes, and methods available for developers.

## Table of Contents
- [Architecture Overview](#architecture-overview)
- [Core Interfaces](#core-interfaces)
- [Application Commands](#application-commands)
- [Plugin Architecture](#plugin-architecture)
- [Data Models](#data-models)
- [Error Handling](#error-handling)
- [Usage Examples](#usage-examples)

## Architecture Overview

The MedicalAI Thesis Suite follows Clean Architecture principles with the following layers:

### Layer Structure
```
┌─────────────────────────────────────┐
│           Presentation Layer        │
│         (MedicalAI.UI)             │
├─────────────────────────────────────┤
│          Application Layer          │
│      (MedicalAI.Application)       │
├─────────────────────────────────────┤
│         Infrastructure Layer        │
│     (MedicalAI.Infrastructure)     │
├─────────────────────────────────────┤
│            Core Layer               │
│         (MedicalAI.Core)           │
└─────────────────────────────────────┘
```

### Key Design Patterns
- **CQRS**: Command Query Responsibility Segregation using MediatR
- **Repository Pattern**: Data access abstraction
- **Plugin Architecture**: Extensible AI model integration
- **Dependency Injection**: Loose coupling and testability

## Core Interfaces

### Medical Imaging Interfaces

#### IDicomImportService
Handles DICOM file import and processing.

```csharp
namespace MedicalAI.Core.Imaging
{
    public interface IDicomImportService
    {
        Task<ImportResult> ImportAsync(string path, DicomImportOptions options, CancellationToken ct);
    }
    
    public record DicomImportOptions(bool Anonymize = true);
    public record ImportResult(int StudiesImported, int SeriesImported, int ImagesImported);
}
```

**Parameters:**
- `path`: File or directory path to DICOM files
- `options`: Import configuration options
- `ct`: Cancellation token for async operations

**Returns:**
- `ImportResult`: Statistics about imported DICOM data

**Example Usage:**
```csharp
var importService = serviceProvider.GetService<IDicomImportService>();
var options = new DicomImportOptions(Anonymize: true);
var result = await importService.ImportAsync("/path/to/dicom", options, cancellationToken);
Console.WriteLine($"Imported {result.ImagesImported} images from {result.StudiesImported} studies");
```

#### IDicomAnonymizerService
Provides DICOM anonymization capabilities.

```csharp
namespace MedicalAI.Core.Imaging
{
    public interface IDicomAnonymizerService
    {
        Task<int> AnonymizeInPlaceAsync(IEnumerable<string> filePaths, AnonymizerProfile profile, CancellationToken ct);
    }
    
    public record AnonymizerProfile(string Name);
}
```

**Parameters:**
- `filePaths`: Collection of DICOM file paths to anonymize
- `profile`: Anonymization profile (Basic, Enhanced, Custom)
- `ct`: Cancellation token

**Returns:**
- `int`: Number of files successfully anonymized

#### IVolumeStore
Manages 3D medical volume data storage and retrieval.

```csharp
namespace MedicalAI.Core.Imaging
{
    public interface IVolumeStore
    {
        Task<Volume3D> LoadAsync(ImageRef imageRef, CancellationToken ct);
        Task SaveMaskAsync(ImageRef imageRef, Mask3D mask, CancellationToken ct);
    }
    
    public record ImageRef(string Modality, string FilePath, string? SeriesInstanceUid, int? InstanceNumber);
}
```

### AI/ML Interfaces

#### ISegmentationEngine
Provides medical image segmentation capabilities.

```csharp
namespace MedicalAI.Core.ML
{
    public interface ISegmentationEngine
    {
        Task<SegmentationResult> RunAsync(Volume3D volume, SegmentationOptions options, CancellationToken ct);
    }
    
    public record SegmentationOptions(string ModelPath, float Threshold = 0.5f);
    public record SegmentationResult(Mask3D Mask, Dictionary<int,string> Labels);
}
```

**Parameters:**
- `volume`: 3D medical image volume
- `options`: Segmentation configuration
- `ct`: Cancellation token

**Returns:**
- `SegmentationResult`: Segmentation mask and label mappings

**Example Usage:**
```csharp
var segmentationEngine = serviceProvider.GetService<ISegmentationEngine>();
var options = new SegmentationOptions("models/cardiac_seg.onnx", 0.7f);
var result = await segmentationEngine.RunAsync(volume, options, cancellationToken);

// Access segmentation results
var mask = result.Mask;
var labels = result.Labels; // Dictionary mapping label IDs to names
```

#### IClassificationEngine
Provides graph-based medical image classification.

```csharp
namespace MedicalAI.Core.ML
{
    public interface IClassificationEngine
    {
        Task<ClassificationResult> PredictAsync(GraphDescriptor graph, CancellationToken ct);
    }
    
    public record ClassificationResult(Dictionary<string, float> Probabilities, Dictionary<int, float>? NodeImportances = null);
}
```

**Parameters:**
- `graph`: Graph representation of medical image features
- `ct`: Cancellation token

**Returns:**
- `ClassificationResult`: Classification probabilities and feature importance

#### IKnowledgeDistillationService
Manages multi-teacher knowledge distillation workflows.

```csharp
namespace MedicalAI.Core.ML
{
    public interface IKnowledgeDistillationService
    {
        Task<DistillationRunId> StartAsync(DistillationConfig config, CancellationToken ct);
        Task<RunStatus> GetStatusAsync(DistillationRunId id, CancellationToken ct);
        Task<ModelInfo> GetStudentModelAsync(DistillationRunId id, CancellationToken ct);
    }
    
    public record DistillationConfig(string DatasetPath, string[] TeacherModelPaths, string StudentModelPath);
    public record DistillationRunId(string Value);
    public enum RunState { Pending, Running, Completed, Failed }
    public record RunStatus(RunState State, string? Message = null);
    public record ModelInfo(string Path, string Sha256, string? Notes);
}
```

#### INlpReasoningService
Provides Ukrainian medical natural language processing.

```csharp
namespace MedicalAI.Core.ML
{
    public interface INlpReasoningService
    {
        Task<NlpSummary> SummarizeAsync(CaseContext context, CancellationToken ct);
        Task<Sentiment> AnalyzeSentimentAsync(string text, CancellationToken ct);
    }
    
    public enum Sentiment { Negative, Neutral, Positive }
    public record CaseContext(string PatientPseudoId, string StudyId, string? Notes, SegmentationResult? Segmentation, ClassificationResult? Classification);
    public record NlpSummary(string Text, Sentiment Sentiment);
}
```

## Application Commands

The application layer uses the CQRS pattern with MediatR for command handling.

### ImportDicomCommand
Imports DICOM files into the system.

```csharp
namespace MedicalAI.Application.Commands
{
    public record ImportDicomCommand(string Path, bool Anonymize) : IRequest<ImportResult>;
    
    public class ImportDicomHandler : IRequestHandler<ImportDicomCommand, ImportResult>
    {
        private readonly IDicomImportService _importer;
        
        public ImportDicomHandler(IDicomImportService importer) 
        { 
            _importer = importer; 
        }
        
        public Task<ImportResult> Handle(ImportDicomCommand request, CancellationToken cancellationToken)
            => _importer.ImportAsync(request.Path, new DicomImportOptions(request.Anonymize), cancellationToken);
    }
}
```

**Usage:**
```csharp
var mediator = serviceProvider.GetService<IMediator>();
var command = new ImportDicomCommand("/path/to/dicom", true);
var result = await mediator.Send(command, cancellationToken);
```

### RunSegmentationCommand
Executes medical image segmentation.

```csharp
namespace MedicalAI.Application.Commands
{
    public record RunSegmentationCommand(Volume3D Volume, string ModelPath, float Threshold) : IRequest<SegmentationResult>;
    
    public class RunSegmentationHandler : IRequestHandler<RunSegmentationCommand, SegmentationResult>
    {
        private readonly ISegmentationEngine _engine;
        
        public RunSegmentationHandler(ISegmentationEngine engine)
        { 
            _engine = engine; 
        }
        
        public Task<SegmentationResult> Handle(RunSegmentationCommand request, CancellationToken ct)
            => _engine.RunAsync(request.Volume, new SegmentationOptions(request.ModelPath, request.Threshold), ct);
    }
}
```

### RunClassificationCommand
Performs graph-based classification.

```csharp
namespace MedicalAI.Application.Commands
{
    public record RunClassificationCommand(GraphDescriptor Graph) : IRequest<ClassificationResult>;
    
    public class RunClassificationHandler : IRequestHandler<RunClassificationCommand, ClassificationResult>
    {
        private readonly IClassificationEngine _engine;
        
        public RunClassificationHandler(IClassificationEngine engine)
        { 
            _engine = engine; 
        }
        
        public Task<ClassificationResult> Handle(RunClassificationCommand request, CancellationToken ct)
            => _engine.PredictAsync(request.Graph, ct);
    }
}
```

### GenerateNlpSummaryCommand
Generates Ukrainian medical text summaries.

```csharp
namespace MedicalAI.Application.Commands
{
    public record GenerateNlpSummaryCommand(CaseContext Context) : IRequest<NlpSummary>;
    
    public class GenerateNlpSummaryHandler : IRequestHandler<GenerateNlpSummaryCommand, NlpSummary>
    {
        private readonly INlpReasoningService _nlp;
        
        public GenerateNlpSummaryHandler(INlpReasoningService nlp)
        { 
            _nlp = nlp; 
        }
        
        public Task<NlpSummary> Handle(GenerateNlpSummaryCommand request, CancellationToken ct)
            => _nlp.SummarizeAsync(request.Context, ct);
    }
}
```

## Plugin Architecture

The system supports extensible AI model plugins through a well-defined interface system.

### Plugin Interface Implementation

#### Segmentation Plugin Example
```csharp
namespace Segmentation.SKIFSeg
{
    public class SkifSegEngine : ISegmentationEngine
    {
        private readonly MockSegmentationEngine _fallback = new();
        
        public Task<SegmentationResult> RunAsync(Volume3D volume, SegmentationOptions options, CancellationToken ct)
            => _fallback.RunAsync(volume, options, ct); // Can be replaced with ONNX runner
    }
    
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSkifSeg(this IServiceCollection services)
        {
            services.AddSingleton<ISegmentationEngine, SkifSegEngine>();
            return services;
        }
    }
}
```

#### Classification Plugin Example
```csharp
namespace Classification.KIGCN
{
    public class KigcnEngine : IClassificationEngine
    {
        private readonly MockClassificationEngine _fallback = new();
        
        public Task<ClassificationResult> PredictAsync(GraphDescriptor graph, CancellationToken ct)
            => _fallback.PredictAsync(graph, ct);
    }
}
```

#### NLP Plugin Example
```csharp
namespace NLP.MedReasoning.UA
{
    public class UkrainianNlpService : INlpReasoningService
    {
        private readonly SimpleNlpUAService _impl = new();
        
        public Task<NlpSummary> SummarizeAsync(CaseContext context, CancellationToken ct) 
            => _impl.SummarizeAsync(context, ct);
            
        public Task<Sentiment> AnalyzeSentimentAsync(string text, CancellationToken ct) 
            => _impl.AnalyzeSentimentAsync(text, ct);
    }
}
```

### Plugin Registration
Plugins are registered in the dependency injection container:

```csharp
// In Startup.cs or Program.cs
services.AddSkifSeg();
services.AddSingleton<IClassificationEngine, KigcnEngine>();
services.AddSingleton<INlpReasoningService, UkrainianNlpService>();
```

## Data Models

### Core Domain Models

#### Medical Data Models
```csharp
namespace MedicalAI.Core
{
    public record Patient(Guid Id, string PseudoId);
    public record Study(Guid Id, Guid PatientId, string StudyInstanceUid, DateTime CreatedUtc);
    public record Series(Guid Id, Guid StudyId, string SeriesInstanceUid, string Modality, string? Path);
}
```

#### Volume and Mask Models
```csharp
namespace MedicalAI.Core
{
    public record Volume3D(int Width, int Height, int Depth, float VoxX, float VoxY, float VoxZ, byte[] Voxels);
    public record Mask3D(int Width, int Height, int Depth, byte[] Labels);
}
```

#### Graph Models
```csharp
namespace MedicalAI.Core
{
    public record GraphNode(int Id, float[] Features);
    public record GraphEdge(int Source, int Target);
    public record GraphDescriptor(List<GraphNode> Nodes, List<GraphEdge> Edges);
}
```

### Utility Classes

#### Segmentation Metrics
```csharp
namespace MedicalAI.Core.Math
{
    public static class SegmentationMetrics
    {
        /// <summary>
        /// Calculates Dice coefficient between two binary masks
        /// </summary>
        /// <param name="a">First binary mask</param>
        /// <param name="b">Second binary mask</param>
        /// <returns>Dice coefficient (0.0 to 1.0)</returns>
        public static double Dice(byte[] a, byte[] b);
        
        /// <summary>
        /// Calculates Intersection over Union (IoU) between two binary masks
        /// </summary>
        /// <param name="a">First binary mask</param>
        /// <param name="b">Second binary mask</param>
        /// <returns>IoU coefficient (0.0 to 1.0)</returns>
        public static double IoU(byte[] a, byte[] b);
    }
}
```

## Error Handling

### Exception Types
The API uses standard .NET exception types with specific error messages:

- `ArgumentException`: Invalid parameters or configuration
- `InvalidOperationException`: Invalid state or operation
- `FileNotFoundException`: Missing model files or data
- `NotSupportedException`: Unsupported file formats or operations
- `OperationCanceledException`: Cancelled operations

### Error Response Format
```csharp
public class ApiException : Exception
{
    public string ErrorCode { get; }
    public Dictionary<string, object> Details { get; }
    
    public ApiException(string errorCode, string message, Dictionary<string, object>? details = null) 
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details ?? new Dictionary<string, object>();
    }
}
```

### Common Error Codes
- `DICOM_INVALID_FORMAT`: Invalid DICOM file format
- `MODEL_NOT_FOUND`: AI model file not found
- `INSUFFICIENT_MEMORY`: Not enough memory for operation
- `PROCESSING_FAILED`: General processing failure
- `VALIDATION_ERROR`: Input validation failure

## Usage Examples

### Complete Workflow Example
```csharp
public class MedicalImageAnalysisWorkflow
{
    private readonly IMediator _mediator;
    private readonly IVolumeStore _volumeStore;
    
    public MedicalImageAnalysisWorkflow(IMediator mediator, IVolumeStore volumeStore)
    {
        _mediator = mediator;
        _volumeStore = volumeStore;
    }
    
    public async Task<AnalysisResult> AnalyzeImageAsync(string dicomPath, CancellationToken ct)
    {
        // Step 1: Import DICOM
        var importCommand = new ImportDicomCommand(dicomPath, true);
        var importResult = await _mediator.Send(importCommand, ct);
        
        // Step 2: Load volume
        var imageRef = new ImageRef("MR", dicomPath, null, null);
        var volume = await _volumeStore.LoadAsync(imageRef, ct);
        
        // Step 3: Run segmentation
        var segmentationCommand = new RunSegmentationCommand(volume, "models/cardiac.onnx", 0.7f);
        var segmentationResult = await _mediator.Send(segmentationCommand, ct);
        
        // Step 4: Create graph from segmentation
        var graph = CreateGraphFromSegmentation(volume, segmentationResult);
        
        // Step 5: Run classification
        var classificationCommand = new RunClassificationCommand(graph);
        var classificationResult = await _mediator.Send(classificationCommand, ct);
        
        // Step 6: Generate NLP summary
        var context = new CaseContext("PATIENT_001", "STUDY_001", null, segmentationResult, classificationResult);
        var nlpCommand = new GenerateNlpSummaryCommand(context);
        var nlpResult = await _mediator.Send(nlpCommand, ct);
        
        return new AnalysisResult(segmentationResult, classificationResult, nlpResult);
    }
    
    private GraphDescriptor CreateGraphFromSegmentation(Volume3D volume, SegmentationResult segmentation)
    {
        // Implementation to convert segmentation to graph representation
        var nodes = new List<GraphNode>();
        var edges = new List<GraphEdge>();
        
        // Extract features from segmented regions
        // Create spatial relationships between regions
        
        return new GraphDescriptor(nodes, edges);
    }
}

public record AnalysisResult(SegmentationResult Segmentation, ClassificationResult Classification, NlpSummary Summary);
```

### Plugin Development Example
```csharp
// Custom segmentation plugin
public class CustomSegmentationEngine : ISegmentationEngine
{
    private readonly IOnnxModelRunner _modelRunner;
    private readonly ILogger<CustomSegmentationEngine> _logger;
    
    public CustomSegmentationEngine(IOnnxModelRunner modelRunner, ILogger<CustomSegmentationEngine> logger)
    {
        _modelRunner = modelRunner;
        _logger = logger;
    }
    
    public async Task<SegmentationResult> RunAsync(Volume3D volume, SegmentationOptions options, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Starting segmentation with model: {ModelPath}", options.ModelPath);
            
            // Preprocess volume
            var preprocessedData = PreprocessVolume(volume);
            
            // Run ONNX model
            var modelOutput = await _modelRunner.RunAsync(options.ModelPath, preprocessedData, ct);
            
            // Postprocess results
            var mask = PostprocessOutput(modelOutput, volume.Width, volume.Height, volume.Depth, options.Threshold);
            
            // Create label mapping
            var labels = new Dictionary<int, string>
            {
                { 0, "Background" },
                { 1, "Left Ventricle" },
                { 2, "Right Ventricle" },
                { 3, "Myocardium" }
            };
            
            _logger.LogInformation("Segmentation completed successfully");
            return new SegmentationResult(mask, labels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Segmentation failed");
            throw new ApiException("SEGMENTATION_FAILED", "Failed to run segmentation", new Dictionary<string, object>
            {
                { "ModelPath", options.ModelPath },
                { "VolumeSize", $"{volume.Width}x{volume.Height}x{volume.Depth}" }
            });
        }
    }
    
    private float[] PreprocessVolume(Volume3D volume)
    {
        // Normalize intensity values
        // Resize if necessary
        // Convert to model input format
        return new float[volume.Width * volume.Height * volume.Depth];
    }
    
    private Mask3D PostprocessOutput(float[] output, int width, int height, int depth, float threshold)
    {
        var labels = new byte[output.Length];
        for (int i = 0; i < output.Length; i++)
        {
            labels[i] = output[i] > threshold ? (byte)1 : (byte)0;
        }
        return new Mask3D(width, height, depth, labels);
    }
}
```

### Dependency Injection Setup
```csharp
// Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Core services
    services.AddMediatR(typeof(ImportDicomCommand));
    services.AddSingleton<IDicomImportService, DicomImportService>();
    services.AddSingleton<IVolumeStore, FileSystemVolumeStore>();
    
    // AI/ML services
    services.AddSkifSeg(); // Extension method from plugin
    services.AddSingleton<IClassificationEngine, KigcnEngine>();
    services.AddSingleton<INlpReasoningService, UkrainianNlpService>();
    services.AddSingleton<IKnowledgeDistillationService, MultiTeacherDistillationService>();
    
    // Infrastructure services
    services.AddDbContext<MedicalAIDbContext>(options =>
        options.UseSqlite(connectionString));
    
    // Logging
    services.AddLogging(builder =>
        builder.AddSerilog());
}
```

This API documentation provides comprehensive coverage of all interfaces, commands, and usage patterns in the MedicalAI Thesis Suite, enabling developers to effectively integrate with and extend the system.