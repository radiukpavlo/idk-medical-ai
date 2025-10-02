# Plugin Development Guide

This guide provides detailed instructions for developing AI model plugins for the MedicalAI Thesis Suite, including examples, best practices, and integration patterns.

## Table of Contents
- [Plugin Architecture Overview](#plugin-architecture-overview)
- [Creating Your First Plugin](#creating-your-first-plugin)
- [Plugin Types and Interfaces](#plugin-types-and-interfaces)
- [ONNX Model Integration](#onnx-model-integration)
- [Advanced Plugin Features](#advanced-plugin-features)
- [Testing and Validation](#testing-and-validation)
- [Deployment and Distribution](#deployment-and-distribution)
- [Best Practices](#best-practices)

## Plugin Architecture Overview

### Plugin System Design

The MedicalAI Thesis Suite uses a flexible plugin architecture that allows developers to extend the system with custom AI models and processing algorithms.

```
┌─────────────────────────────────────────────────────────────┐
│                    Host Application                          │
├─────────────────────────────────────────────────────────────┤
│                   Plugin Manager                            │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │   Plugin A  │  │   Plugin B  │  │   Plugin C  │        │
│  │ (Segment.)  │  │ (Classify.) │  │   (NLP)     │        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
├─────────────────────────────────────────────────────────────┤
│                  Core Interfaces                            │
│  ISegmentationEngine | IClassificationEngine | INlpService │
└─────────────────────────────────────────────────────────────┘
```

### Key Principles

1. **Interface-Based**: Plugins implement well-defined interfaces
2. **Dependency Injection**: Plugins are registered in the DI container
3. **Isolation**: Plugins are isolated from each other
4. **Extensibility**: New plugin types can be added easily
5. **Hot-Swappable**: Different implementations can be swapped at runtime

### Plugin Lifecycle

```
Plugin Discovery → Interface Validation → Registration → Instantiation → Execution → Disposal
```

## Creating Your First Plugin

### Step 1: Set Up Plugin Project

Create a new .NET class library project:

```bash
dotnet new classlib -n MyCustomPlugin
cd MyCustomPlugin
```

Add required package references:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\MedicalAI.Core\MedicalAI.Core.csproj" />
    <ProjectReference Include="..\MedicalAI.Infrastructure\MedicalAI.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="Microsoft.ML.OnnxRuntime" Version="1.23.0" />
  </ItemGroup>
</Project>
```

### Step 2: Implement Plugin Interface

Create a simple segmentation plugin:

```csharp
using MedicalAI.Core;
using MedicalAI.Core.ML;
using Microsoft.Extensions.Logging;

namespace MyCustomPlugin
{
    public class CustomSegmentationEngine : ISegmentationEngine
    {
        private readonly ILogger<CustomSegmentationEngine> _logger;
        
        public CustomSegmentationEngine(ILogger<CustomSegmentationEngine> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public async Task<SegmentationResult> RunAsync(
            Volume3D volume, 
            SegmentationOptions options, 
            CancellationToken ct)
        {
            _logger.LogInformation("Starting custom segmentation for volume {Width}x{Height}x{Depth}", 
                volume.Width, volume.Height, volume.Depth);
            
            try
            {
                // Validate inputs
                ValidateInputs(volume, options);
                
                // Process the volume
                var mask = await ProcessVolumeAsync(volume, options, ct);
                
                // Create label mapping
                var labels = CreateLabelMapping();
                
                _logger.LogInformation("Custom segmentation completed successfully");
                return new SegmentationResult(mask, labels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Custom segmentation failed");
                throw;
            }
        }
        
        private void ValidateInputs(Volume3D volume, SegmentationOptions options)
        {
            if (volume.Width <= 0 || volume.Height <= 0 || volume.Depth <= 0)
                throw new ArgumentException("Invalid volume dimensions");
                
            if (string.IsNullOrEmpty(options.ModelPath))
                throw new ArgumentException("Model path is required");
                
            if (options.Threshold < 0 || options.Threshold > 1)
                throw new ArgumentException("Threshold must be between 0 and 1");
        }
        
        private async Task<Mask3D> ProcessVolumeAsync(
            Volume3D volume, 
            SegmentationOptions options, 
            CancellationToken ct)
        {
            // For demonstration, create a simple mock segmentation
            // In a real plugin, this would use ONNX Runtime or other ML framework
            
            var maskData = new byte[volume.Voxels.Length];
            
            // Simple thresholding as example
            for (int i = 0; i < volume.Voxels.Length; i++)
            {
                ct.ThrowIfCancellationRequested();
                
                // Convert voxel value to normalized intensity
                var intensity = volume.Voxels[i] / 255.0f;
                
                // Apply threshold
                maskData[i] = intensity > options.Threshold ? (byte)1 : (byte)0;
            }
            
            // Simulate processing time
            await Task.Delay(100, ct);
            
            return new Mask3D(volume.Width, volume.Height, volume.Depth, maskData);
        }
        
        private Dictionary<int, string> CreateLabelMapping()
        {
            return new Dictionary<int, string>
            {
                { 0, "Background" },
                { 1, "Foreground" }
            };
        }
    }
}
```

### Step 3: Create Registration Extension

Add a service collection extension for easy registration:

```csharp
using Microsoft.Extensions.DependencyInjection;
using MedicalAI.Core.ML;

namespace MyCustomPlugin
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCustomSegmentation(this IServiceCollection services)
        {
            services.AddSingleton<ISegmentationEngine, CustomSegmentationEngine>();
            return services;
        }
        
        public static IServiceCollection AddCustomSegmentation(
            this IServiceCollection services, 
            Action<CustomSegmentationOptions> configure)
        {
            services.Configure(configure);
            services.AddSingleton<ISegmentationEngine, CustomSegmentationEngine>();
            return services;
        }
    }
    
    public class CustomSegmentationOptions
    {
        public string DefaultModelPath { get; set; } = "models/custom_segmentation.onnx";
        public float DefaultThreshold { get; set; } = 0.5f;
        public bool EnableGpuAcceleration { get; set; } = true;
    }
}
```

### Step 4: Register Plugin in Host Application

In the host application's startup code:

```csharp
// Program.cs or Startup.cs
services.AddCustomSegmentation(options =>
{
    options.DefaultModelPath = "models/my_custom_model.onnx";
    options.DefaultThreshold = 0.7f;
    options.EnableGpuAcceleration = true;
});
```

## Plugin Types and Interfaces

### Segmentation Plugins

Implement `ISegmentationEngine` for medical image segmentation:

```csharp
public interface ISegmentationEngine
{
    Task<SegmentationResult> RunAsync(Volume3D volume, SegmentationOptions options, CancellationToken ct);
}

public record SegmentationOptions(string ModelPath, float Threshold = 0.5f);
public record SegmentationResult(Mask3D Mask, Dictionary<int, string> Labels);
```

**Example: Advanced Segmentation Plugin**

```csharp
public class AdvancedSegmentationEngine : ISegmentationEngine
{
    private readonly IOnnxModelRunner _modelRunner;
    private readonly IImagePreprocessor _preprocessor;
    private readonly ILogger<AdvancedSegmentationEngine> _logger;
    
    public AdvancedSegmentationEngine(
        IOnnxModelRunner modelRunner,
        IImagePreprocessor preprocessor,
        ILogger<AdvancedSegmentationEngine> logger)
    {
        _modelRunner = modelRunner;
        _preprocessor = preprocessor;
        _logger = logger;
    }
    
    public async Task<SegmentationResult> RunAsync(
        Volume3D volume, 
        SegmentationOptions options, 
        CancellationToken ct)
    {
        // Multi-step processing pipeline
        var preprocessed = await _preprocessor.PreprocessAsync(volume, ct);
        var modelOutput = await _modelRunner.RunAsync(options.ModelPath, preprocessed, ct);
        var postprocessed = await PostprocessAsync(modelOutput, volume, options.Threshold, ct);
        
        return new SegmentationResult(postprocessed.Mask, postprocessed.Labels);
    }
    
    private async Task<(Mask3D Mask, Dictionary<int, string> Labels)> PostprocessAsync(
        float[] modelOutput, 
        Volume3D originalVolume, 
        float threshold, 
        CancellationToken ct)
    {
        // Advanced postprocessing: morphological operations, connected components, etc.
        var mask = ApplyThreshold(modelOutput, threshold);
        mask = ApplyMorphologicalOperations(mask);
        mask = FilterConnectedComponents(mask, minSize: 100);
        
        var labels = new Dictionary<int, string>
        {
            { 0, "Background" },
            { 1, "Left Ventricle" },
            { 2, "Right Ventricle" },
            { 3, "Myocardium" }
        };
        
        return (new Mask3D(originalVolume.Width, originalVolume.Height, originalVolume.Depth, mask), labels);
    }
}
```

### Classification Plugins

Implement `IClassificationEngine` for medical image classification:

```csharp
public interface IClassificationEngine
{
    Task<ClassificationResult> PredictAsync(GraphDescriptor graph, CancellationToken ct);
}

public record ClassificationResult(Dictionary<string, float> Probabilities, Dictionary<int, float>? NodeImportances = null);
```

**Example: Graph-Based Classification Plugin**

```csharp
public class GraphClassificationEngine : IClassificationEngine
{
    private readonly IOnnxModelRunner _modelRunner;
    private readonly IGraphFeatureExtractor _featureExtractor;
    private readonly ILogger<GraphClassificationEngine> _logger;
    
    public GraphClassificationEngine(
        IOnnxModelRunner modelRunner,
        IGraphFeatureExtractor featureExtractor,
        ILogger<GraphClassificationEngine> logger)
    {
        _modelRunner = modelRunner;
        _featureExtractor = featureExtractor;
        _logger = logger;
    }
    
    public async Task<ClassificationResult> PredictAsync(GraphDescriptor graph, CancellationToken ct)
    {
        _logger.LogInformation("Starting graph classification with {NodeCount} nodes", graph.Nodes.Count);
        
        // Extract graph features
        var features = await _featureExtractor.ExtractAsync(graph, ct);
        
        // Run classification model
        var predictions = await _modelRunner.RunAsync("models/graph_classifier.onnx", features, ct);
        
        // Process results
        var probabilities = ProcessPredictions(predictions);
        var nodeImportances = CalculateNodeImportances(graph, predictions);
        
        return new ClassificationResult(probabilities, nodeImportances);
    }
    
    private Dictionary<string, float> ProcessPredictions(float[] predictions)
    {
        var classes = new[] { "Normal", "Abnormal", "Uncertain" };
        var probabilities = new Dictionary<string, float>();
        
        for (int i = 0; i < Math.Min(classes.Length, predictions.Length); i++)
        {
            probabilities[classes[i]] = predictions[i];
        }
        
        return probabilities;
    }
    
    private Dictionary<int, float> CalculateNodeImportances(GraphDescriptor graph, float[] predictions)
    {
        // Calculate feature importance for each node
        var importances = new Dictionary<int, float>();
        
        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            // Simplified importance calculation
            var node = graph.Nodes[i];
            var importance = node.Features.Sum() * predictions.Max();
            importances[node.Id] = importance;
        }
        
        return importances;
    }
}
```

### NLP Plugins

Implement `INlpReasoningService` for natural language processing:

```csharp
public interface INlpReasoningService
{
    Task<NlpSummary> SummarizeAsync(CaseContext context, CancellationToken ct);
    Task<Sentiment> AnalyzeSentimentAsync(string text, CancellationToken ct);
}

public enum Sentiment { Negative, Neutral, Positive }
public record NlpSummary(string Text, Sentiment Sentiment);
```

**Example: Ukrainian Medical NLP Plugin**

```csharp
public class UkrainianMedicalNlpEngine : INlpReasoningService
{
    private readonly ITokenizer _tokenizer;
    private readonly IOnnxModelRunner _modelRunner;
    private readonly IMedicalTerminologyService _terminology;
    private readonly ILogger<UkrainianMedicalNlpEngine> _logger;
    
    public UkrainianMedicalNlpEngine(
        ITokenizer tokenizer,
        IOnnxModelRunner modelRunner,
        IMedicalTerminologyService terminology,
        ILogger<UkrainianMedicalNlpEngine> logger)
    {
        _tokenizer = tokenizer;
        _modelRunner = modelRunner;
        _terminology = terminology;
        _logger = logger;
    }
    
    public async Task<NlpSummary> SummarizeAsync(CaseContext context, CancellationToken ct)
    {
        _logger.LogInformation("Generating Ukrainian medical summary for patient {PatientId}", context.PatientPseudoId);
        
        // Combine all available information
        var combinedText = BuildContextText(context);
        
        // Tokenize Ukrainian text
        var tokens = await _tokenizer.TokenizeAsync(combinedText, "uk", ct);
        
        // Extract medical entities
        var entities = await ExtractMedicalEntitiesAsync(tokens, ct);
        
        // Generate summary using NLP model
        var summaryTokens = await _modelRunner.RunAsync("models/ukrainian_medical_summarizer.onnx", tokens, ct);
        var summaryText = await _tokenizer.DetokenizeAsync(summaryTokens, "uk", ct);
        
        // Analyze sentiment
        var sentiment = await AnalyzeSentimentAsync(summaryText, ct);
        
        // Enhance with medical terminology
        var enhancedSummary = await EnhanceWithTerminologyAsync(summaryText, entities, ct);
        
        return new NlpSummary(enhancedSummary, sentiment);
    }
    
    public async Task<Sentiment> AnalyzeSentimentAsync(string text, CancellationToken ct)
    {
        var tokens = await _tokenizer.TokenizeAsync(text, "uk", ct);
        var sentimentScores = await _modelRunner.RunAsync("models/ukrainian_sentiment.onnx", tokens, ct);
        
        // Convert scores to sentiment
        var maxIndex = Array.IndexOf(sentimentScores, sentimentScores.Max());
        return (Sentiment)maxIndex;
    }
    
    private string BuildContextText(CaseContext context)
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(context.Notes))
            parts.Add($"Примітки: {context.Notes}");
        
        if (context.Segmentation != null)
            parts.Add($"Сегментація: виявлено {context.Segmentation.Labels.Count} структур");
        
        if (context.Classification != null)
        {
            var topClass = context.Classification.Probabilities.OrderByDescending(p => p.Value).First();
            parts.Add($"Класифікація: {topClass.Key} ({topClass.Value:P1})");
        }
        
        return string.Join(". ", parts);
    }
    
    private async Task<List<MedicalEntity>> ExtractMedicalEntitiesAsync(float[] tokens, CancellationToken ct)
    {
        var entityScores = await _modelRunner.RunAsync("models/ukrainian_medical_ner.onnx", tokens, ct);
        
        // Process NER results and map to medical entities
        var entities = new List<MedicalEntity>();
        
        // Implementation would parse entity scores and create MedicalEntity objects
        
        return entities;
    }
    
    private async Task<string> EnhanceWithTerminologyAsync(string text, List<MedicalEntity> entities, CancellationToken ct)
    {
        var enhancedText = text;
        
        foreach (var entity in entities)
        {
            var definition = await _terminology.GetDefinitionAsync(entity.Term, "uk", ct);
            if (!string.IsNullOrEmpty(definition))
            {
                enhancedText = enhancedText.Replace(entity.Term, $"{entity.Term} ({definition})");
            }
        }
        
        return enhancedText;
    }
}

public record MedicalEntity(string Term, string Type, float Confidence);
```

## ONNX Model Integration

### ONNX Runtime Setup

Create an ONNX model runner service:

```csharp
public interface IOnnxModelRunner
{
    Task<float[]> RunAsync(string modelPath, float[] input, CancellationToken ct);
    Task<Dictionary<string, float[]>> RunAsync(string modelPath, Dictionary<string, float[]> inputs, CancellationToken ct);
}

public class OnnxModelRunner : IOnnxModelRunner, IDisposable
{
    private readonly ConcurrentDictionary<string, InferenceSession> _sessions;
    private readonly SessionOptions _sessionOptions;
    private readonly ILogger<OnnxModelRunner> _logger;
    
    public OnnxModelRunner(ILogger<OnnxModelRunner> logger)
    {
        _sessions = new ConcurrentDictionary<string, InferenceSession>();
        _sessionOptions = new SessionOptions();
        _logger = logger;
        
        // Configure ONNX Runtime
        ConfigureSessionOptions();
    }
    
    private void ConfigureSessionOptions()
    {
        // Enable GPU if available
        if (IsGpuAvailable())
        {
            _sessionOptions.AppendExecutionProvider_DML(0);
            _logger.LogInformation("GPU acceleration enabled for ONNX Runtime");
        }
        
        // Set optimization level
        _sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        
        // Set thread count
        _sessionOptions.IntraOpNumThreads = Environment.ProcessorCount;
    }
    
    public async Task<float[]> RunAsync(string modelPath, float[] input, CancellationToken ct)
    {
        var session = GetOrCreateSession(modelPath);
        
        // Get input metadata
        var inputMeta = session.InputMetadata.First();
        var inputName = inputMeta.Key;
        var inputShape = inputMeta.Value.Dimensions.ToArray();
        
        // Create input tensor
        var inputTensor = new DenseTensor<float>(input, inputShape);
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
        };
        
        // Run inference
        using var results = session.Run(inputs);
        var output = results.First().AsEnumerable<float>().ToArray();
        
        return output;
    }
    
    public async Task<Dictionary<string, float[]>> RunAsync(string modelPath, Dictionary<string, float[]> inputs, CancellationToken ct)
    {
        var session = GetOrCreateSession(modelPath);
        
        // Create ONNX inputs
        var onnxInputs = new List<NamedOnnxValue>();
        foreach (var input in inputs)
        {
            var inputMeta = session.InputMetadata[input.Key];
            var inputShape = inputMeta.Dimensions.ToArray();
            var tensor = new DenseTensor<float>(input.Value, inputShape);
            onnxInputs.Add(NamedOnnxValue.CreateFromTensor(input.Key, tensor));
        }
        
        // Run inference
        using var results = session.Run(onnxInputs);
        
        // Extract outputs
        var outputs = new Dictionary<string, float[]>();
        foreach (var result in results)
        {
            outputs[result.Name] = result.AsEnumerable<float>().ToArray();
        }
        
        return outputs;
    }
    
    private InferenceSession GetOrCreateSession(string modelPath)
    {
        return _sessions.GetOrAdd(modelPath, path =>
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"ONNX model not found: {path}");
            
            _logger.LogInformation("Loading ONNX model: {ModelPath}", path);
            return new InferenceSession(path, _sessionOptions);
        });
    }
    
    private bool IsGpuAvailable()
    {
        try
        {
            // Check if DirectML provider is available
            return OrtEnv.Instance().GetAvailableProviders().Contains("DmlExecutionProvider");
        }
        catch
        {
            return false;
        }
    }
    
    public void Dispose()
    {
        foreach (var session in _sessions.Values)
        {
            session?.Dispose();
        }
        _sessions.Clear();
        _sessionOptions?.Dispose();
    }
}
```

### Model Preprocessing and Postprocessing

Create reusable preprocessing components:

```csharp
public interface IImagePreprocessor
{
    Task<float[]> PreprocessAsync(Volume3D volume, CancellationToken ct);
}

public class StandardImagePreprocessor : IImagePreprocessor
{
    private readonly PreprocessingOptions _options;
    
    public StandardImagePreprocessor(IOptions<PreprocessingOptions> options)
    {
        _options = options.Value;
    }
    
    public async Task<float[]> PreprocessAsync(Volume3D volume, CancellationToken ct)
    {
        var processed = volume.Voxels.Select(v => (float)v).ToArray();
        
        // Normalize intensity values
        if (_options.NormalizeIntensity)
        {
            processed = NormalizeIntensity(processed);
        }
        
        // Resize if needed
        if (_options.TargetSize != null)
        {
            processed = await ResizeVolumeAsync(processed, volume, _options.TargetSize.Value, ct);
        }
        
        // Apply windowing
        if (_options.WindowLevel.HasValue && _options.WindowWidth.HasValue)
        {
            processed = ApplyWindowing(processed, _options.WindowLevel.Value, _options.WindowWidth.Value);
        }
        
        return processed;
    }
    
    private float[] NormalizeIntensity(float[] voxels)
    {
        var mean = voxels.Average();
        var std = MathF.Sqrt(voxels.Select(v => (v - mean) * (v - mean)).Average());
        
        return voxels.Select(v => (v - mean) / std).ToArray();
    }
    
    private async Task<float[]> ResizeVolumeAsync(float[] voxels, Volume3D originalVolume, (int Width, int Height, int Depth) targetSize, CancellationToken ct)
    {
        // Implement trilinear interpolation for volume resizing
        var resized = new float[targetSize.Width * targetSize.Height * targetSize.Depth];
        
        var xRatio = (float)originalVolume.Width / targetSize.Width;
        var yRatio = (float)originalVolume.Height / targetSize.Height;
        var zRatio = (float)originalVolume.Depth / targetSize.Depth;
        
        await Task.Run(() =>
        {
            Parallel.For(0, targetSize.Depth, z =>
            {
                ct.ThrowIfCancellationRequested();
                
                for (int y = 0; y < targetSize.Height; y++)
                {
                    for (int x = 0; x < targetSize.Width; x++)
                    {
                        var srcX = x * xRatio;
                        var srcY = y * yRatio;
                        var srcZ = z * zRatio;
                        
                        var interpolatedValue = TrilinearInterpolation(voxels, originalVolume, srcX, srcY, srcZ);
                        var targetIndex = z * targetSize.Width * targetSize.Height + y * targetSize.Width + x;
                        resized[targetIndex] = interpolatedValue;
                    }
                }
            });
        }, ct);
        
        return resized;
    }
    
    private float TrilinearInterpolation(float[] voxels, Volume3D volume, float x, float y, float z)
    {
        // Implement trilinear interpolation
        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var z0 = (int)Math.Floor(z);
        
        var x1 = Math.Min(x0 + 1, volume.Width - 1);
        var y1 = Math.Min(y0 + 1, volume.Height - 1);
        var z1 = Math.Min(z0 + 1, volume.Depth - 1);
        
        var xd = x - x0;
        var yd = y - y0;
        var zd = z - z0;
        
        // Get 8 corner values
        var c000 = GetVoxel(voxels, volume, x0, y0, z0);
        var c001 = GetVoxel(voxels, volume, x0, y0, z1);
        var c010 = GetVoxel(voxels, volume, x0, y1, z0);
        var c011 = GetVoxel(voxels, volume, x0, y1, z1);
        var c100 = GetVoxel(voxels, volume, x1, y0, z0);
        var c101 = GetVoxel(voxels, volume, x1, y0, z1);
        var c110 = GetVoxel(voxels, volume, x1, y1, z0);
        var c111 = GetVoxel(voxels, volume, x1, y1, z1);
        
        // Interpolate
        var c00 = c000 * (1 - xd) + c100 * xd;
        var c01 = c001 * (1 - xd) + c101 * xd;
        var c10 = c010 * (1 - xd) + c110 * xd;
        var c11 = c011 * (1 - xd) + c111 * xd;
        
        var c0 = c00 * (1 - yd) + c10 * yd;
        var c1 = c01 * (1 - yd) + c11 * yd;
        
        return c0 * (1 - zd) + c1 * zd;
    }
    
    private float GetVoxel(float[] voxels, Volume3D volume, int x, int y, int z)
    {
        var index = z * volume.Width * volume.Height + y * volume.Width + x;
        return index < voxels.Length ? voxels[index] : 0;
    }
    
    private float[] ApplyWindowing(float[] voxels, float windowLevel, float windowWidth)
    {
        var minValue = windowLevel - windowWidth / 2;
        var maxValue = windowLevel + windowWidth / 2;
        
        return voxels.Select(v =>
        {
            if (v <= minValue) return 0f;
            if (v >= maxValue) return 1f;
            return (v - minValue) / windowWidth;
        }).ToArray();
    }
}

public class PreprocessingOptions
{
    public bool NormalizeIntensity { get; set; } = true;
    public (int Width, int Height, int Depth)? TargetSize { get; set; }
    public float? WindowLevel { get; set; }
    public float? WindowWidth { get; set; }
}
```

## Advanced Plugin Features

### Configuration and Options

Create configurable plugins with validation:

```csharp
public class ConfigurableSegmentationEngine : ISegmentationEngine
{
    private readonly SegmentationEngineOptions _options;
    private readonly IOnnxModelRunner _modelRunner;
    private readonly ILogger<ConfigurableSegmentationEngine> _logger;
    
    public ConfigurableSegmentationEngine(
        IOptions<SegmentationEngineOptions> options,
        IOnnxModelRunner modelRunner,
        ILogger<ConfigurableSegmentationEngine> logger)
    {
        _options = options.Value;
        _modelRunner = modelRunner;
        _logger = logger;
        
        ValidateOptions();
    }
    
    public async Task<SegmentationResult> RunAsync(Volume3D volume, SegmentationOptions options, CancellationToken ct)
    {
        // Merge runtime options with configured defaults
        var effectiveOptions = MergeOptions(options);
        
        // Apply preprocessing pipeline
        var preprocessed = await ApplyPreprocessingPipelineAsync(volume, ct);
        
        // Run model with configured settings
        var modelOutput = await _modelRunner.RunAsync(effectiveOptions.ModelPath, preprocessed, ct);
        
        // Apply postprocessing pipeline
        var result = await ApplyPostprocessingPipelineAsync(modelOutput, volume, effectiveOptions, ct);
        
        return result;
    }
    
    private void ValidateOptions()
    {
        if (string.IsNullOrEmpty(_options.DefaultModelPath))
            throw new InvalidOperationException("DefaultModelPath is required");
        
        if (_options.DefaultThreshold < 0 || _options.DefaultThreshold > 1)
            throw new InvalidOperationException("DefaultThreshold must be between 0 and 1");
        
        if (_options.MaxInputSize <= 0)
            throw new InvalidOperationException("MaxInputSize must be positive");
    }
    
    private SegmentationOptions MergeOptions(SegmentationOptions runtimeOptions)
    {
        return new SegmentationOptions(
            ModelPath: !string.IsNullOrEmpty(runtimeOptions.ModelPath) ? runtimeOptions.ModelPath : _options.DefaultModelPath,
            Threshold: runtimeOptions.Threshold != 0.5f ? runtimeOptions.Threshold : _options.DefaultThreshold
        );
    }
    
    private async Task<float[]> ApplyPreprocessingPipelineAsync(Volume3D volume, CancellationToken ct)
    {
        var pipeline = _options.PreprocessingPipeline;
        var data = volume.Voxels.Select(v => (float)v).ToArray();
        
        foreach (var step in pipeline)
        {
            data = await step.ProcessAsync(data, volume, ct);
        }
        
        return data;
    }
    
    private async Task<SegmentationResult> ApplyPostprocessingPipelineAsync(
        float[] modelOutput, 
        Volume3D originalVolume, 
        SegmentationOptions options, 
        CancellationToken ct)
    {
        var pipeline = _options.PostprocessingPipeline;
        var mask = ApplyThreshold(modelOutput, options.Threshold);
        
        foreach (var step in pipeline)
        {
            mask = await step.ProcessAsync(mask, originalVolume, ct);
        }
        
        var labels = _options.LabelMapping ?? CreateDefaultLabels();
        return new SegmentationResult(
            new Mask3D(originalVolume.Width, originalVolume.Height, originalVolume.Depth, mask), 
            labels);
    }
}

public class SegmentationEngineOptions
{
    public string DefaultModelPath { get; set; } = string.Empty;
    public float DefaultThreshold { get; set; } = 0.5f;
    public int MaxInputSize { get; set; } = 512 * 512 * 512;
    public bool EnableGpuAcceleration { get; set; } = true;
    public List<IPreprocessingStep> PreprocessingPipeline { get; set; } = new();
    public List<IPostprocessingStep> PostprocessingPipeline { get; set; } = new();
    public Dictionary<int, string>? LabelMapping { get; set; }
}

public interface IPreprocessingStep
{
    Task<float[]> ProcessAsync(float[] data, Volume3D volume, CancellationToken ct);
}

public interface IPostprocessingStep
{
    Task<byte[]> ProcessAsync(byte[] mask, Volume3D volume, CancellationToken ct);
}
```

### Performance Monitoring

Add performance monitoring to plugins:

```csharp
public class MonitoredSegmentationEngine : ISegmentationEngine
{
    private readonly ISegmentationEngine _innerEngine;
    private readonly IMetricsCollector _metrics;
    private readonly ILogger<MonitoredSegmentationEngine> _logger;
    
    public MonitoredSegmentationEngine(
        ISegmentationEngine innerEngine,
        IMetricsCollector metrics,
        ILogger<MonitoredSegmentationEngine> logger)
    {
        _innerEngine = innerEngine;
        _metrics = metrics;
        _logger = logger;
    }
    
    public async Task<SegmentationResult> RunAsync(Volume3D volume, SegmentationOptions options, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var volumeSize = volume.Width * volume.Height * volume.Depth;
        
        try
        {
            _logger.LogInformation("Starting segmentation monitoring for volume size: {VolumeSize}", volumeSize);
            
            // Monitor memory before processing
            var memoryBefore = GC.GetTotalMemory(false);
            
            // Execute segmentation
            var result = await _innerEngine.RunAsync(volume, options, ct);
            
            // Monitor memory after processing
            var memoryAfter = GC.GetTotalMemory(false);
            var memoryUsed = memoryAfter - memoryBefore;
            
            stopwatch.Stop();
            
            // Record metrics
            _metrics.RecordProcessingTime("segmentation", stopwatch.ElapsedMilliseconds);
            _metrics.RecordMemoryUsage("segmentation", memoryUsed);
            _metrics.RecordVolumeSize("segmentation", volumeSize);
            _metrics.RecordThroughput("segmentation", volumeSize / stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogInformation("Segmentation completed in {ElapsedMs}ms, memory used: {MemoryMB}MB", 
                stopwatch.ElapsedMilliseconds, memoryUsed / 1024 / 1024);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.RecordError("segmentation", ex.GetType().Name);
            _logger.LogError(ex, "Segmentation failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

public interface IMetricsCollector
{
    void RecordProcessingTime(string operation, long milliseconds);
    void RecordMemoryUsage(string operation, long bytes);
    void RecordVolumeSize(string operation, long voxels);
    void RecordThroughput(string operation, double voxelsPerSecond);
    void RecordError(string operation, string errorType);
}
```

### Caching and Resource Management

Implement efficient resource management:

```csharp
public class CachedSegmentationEngine : ISegmentationEngine, IDisposable
{
    private readonly ISegmentationEngine _innerEngine;
    private readonly IMemoryCache _cache;
    private readonly SemaphoreSlim _semaphore;
    private readonly CacheOptions _cacheOptions;
    private bool _disposed;
    
    public CachedSegmentationEngine(
        ISegmentationEngine innerEngine,
        IMemoryCache cache,
        IOptions<CacheOptions> cacheOptions)
    {
        _innerEngine = innerEngine;
        _cache = cache;
        _cacheOptions = cacheOptions.Value;
        _semaphore = new SemaphoreSlim(_cacheOptions.MaxConcurrentOperations);
    }
    
    public async Task<SegmentationResult> RunAsync(Volume3D volume, SegmentationOptions options, CancellationToken ct)
    {
        // Generate cache key
        var cacheKey = GenerateCacheKey(volume, options);
        
        // Check cache first
        if (_cache.TryGetValue(cacheKey, out SegmentationResult? cachedResult))
        {
            return cachedResult!;
        }
        
        // Acquire semaphore to limit concurrent operations
        await _semaphore.WaitAsync(ct);
        
        try
        {
            // Double-check cache after acquiring semaphore
            if (_cache.TryGetValue(cacheKey, out cachedResult))
            {
                return cachedResult!;
            }
            
            // Execute segmentation
            var result = await _innerEngine.RunAsync(volume, options, ct);
            
            // Cache result if enabled
            if (_cacheOptions.EnableCaching)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheOptions.CacheExpiry,
                    Size = EstimateResultSize(result),
                    Priority = CacheItemPriority.Normal
                };
                
                _cache.Set(cacheKey, result, cacheEntryOptions);
            }
            
            return result;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private string GenerateCacheKey(Volume3D volume, SegmentationOptions options)
    {
        // Create hash of volume data and options
        using var sha256 = SHA256.Create();
        
        var volumeHash = sha256.ComputeHash(volume.Voxels);
        var optionsJson = JsonSerializer.Serialize(options);
        var optionsHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(optionsJson));
        
        var combinedHash = volumeHash.Concat(optionsHash).ToArray();
        return Convert.ToBase64String(sha256.ComputeHash(combinedHash));
    }
    
    private long EstimateResultSize(SegmentationResult result)
    {
        // Estimate memory size of result for cache management
        var maskSize = result.Mask.Labels.Length;
        var labelsSize = result.Labels.Sum(kvp => kvp.Key.ToString().Length + kvp.Value.Length) * sizeof(char);
        return maskSize + labelsSize;
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _semaphore?.Dispose();
            _disposed = true;
        }
    }
}

public class CacheOptions
{
    public bool EnableCaching { get; set; } = true;
    public TimeSpan CacheExpiry { get; set; } = TimeSpan.FromMinutes(30);
    public int MaxConcurrentOperations { get; set; } = Environment.ProcessorCount;
    public long MaxCacheSize { get; set; } = 1024 * 1024 * 1024; // 1GB
}
```

## Testing and Validation

### Unit Testing Plugins

Create comprehensive unit tests for plugins:

```csharp
[TestClass]
public class CustomSegmentationEngineTests
{
    private Mock<ILogger<CustomSegmentationEngine>> _mockLogger;
    private CustomSegmentationEngine _engine;
    
    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<CustomSegmentationEngine>>();
        _engine = new CustomSegmentationEngine(_mockLogger.Object);
    }
    
    [TestMethod]
    public async Task RunAsync_ValidInput_ReturnsSegmentationResult()
    {
        // Arrange
        var volume = CreateTestVolume(64, 64, 32);
        var options = new SegmentationOptions("test-model.onnx", 0.5f);
        
        // Act
        var result = await _engine.RunAsync(volume, options, CancellationToken.None);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Mask);
        Assert.AreEqual(volume.Width, result.Mask.Width);
        Assert.AreEqual(volume.Height, result.Mask.Height);
        Assert.AreEqual(volume.Depth, result.Mask.Depth);
        Assert.IsTrue(result.Labels.Count > 0);
    }
    
    [TestMethod]
    public async Task RunAsync_InvalidVolume_ThrowsArgumentException()
    {
        // Arrange
        var volume = CreateTestVolume(0, 0, 0); // Invalid dimensions
        var options = new SegmentationOptions("test-model.onnx", 0.5f);
        
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _engine.RunAsync(volume, options, CancellationToken.None));
    }
    
    [TestMethod]
    public async Task RunAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var volume = CreateTestVolume(64, 64, 32);
        var options = new SegmentationOptions("test-model.onnx", 0.5f);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Act & Assert
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => _engine.RunAsync(volume, options, cts.Token));
    }
    
    [TestMethod]
    public void ValidateInputs_ValidInputs_DoesNotThrow()
    {
        // Arrange
        var volume = CreateTestVolume(64, 64, 32);
        var options = new SegmentationOptions("test-model.onnx", 0.5f);
        
        // Act & Assert - should not throw
        var method = typeof(CustomSegmentationEngine).GetMethod("ValidateInputs", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(_engine, new object[] { volume, options });
    }
    
    private Volume3D CreateTestVolume(int width, int height, int depth)
    {
        var voxels = new byte[width * height * depth];
        var random = new Random(42); // Deterministic for testing
        random.NextBytes(voxels);
        
        return new Volume3D(width, height, depth, 1.0f, 1.0f, 1.0f, voxels);
    }
}
```

### Integration Testing

Test plugin integration with the host application:

```csharp
[TestClass]
public class PluginIntegrationTests
{
    private IServiceProvider _serviceProvider;
    
    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        // Register core services
        services.AddLogging();
        services.AddMemoryCache();
        
        // Register plugin
        services.AddCustomSegmentation();
        
        // Register mock dependencies
        services.AddSingleton<IOnnxModelRunner, MockOnnxModelRunner>();
        
        _serviceProvider = services.BuildServiceProvider();
    }
    
    [TestMethod]
    public async Task SegmentationEngine_RegisteredCorrectly_CanResolveAndExecute()
    {
        // Arrange
        var engine = _serviceProvider.GetRequiredService<ISegmentationEngine>();
        var volume = CreateTestVolume();
        var options = new SegmentationOptions("test-model.onnx", 0.5f);
        
        // Act
        var result = await engine.RunAsync(volume, options, CancellationToken.None);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(engine, typeof(CustomSegmentationEngine));
    }
    
    [TestMethod]
    public async Task MultiplePlugins_RegisteredCorrectly_CanResolveAll()
    {
        // Arrange - register multiple plugins
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCustomSegmentation();
        services.AddSingleton<IClassificationEngine, MockClassificationEngine>();
        services.AddSingleton<INlpReasoningService, MockNlpService>();
        
        var provider = services.BuildServiceProvider();
        
        // Act
        var segmentationEngine = provider.GetRequiredService<ISegmentationEngine>();
        var classificationEngine = provider.GetRequiredService<IClassificationEngine>();
        var nlpService = provider.GetRequiredService<INlpReasoningService>();
        
        // Assert
        Assert.IsNotNull(segmentationEngine);
        Assert.IsNotNull(classificationEngine);
        Assert.IsNotNull(nlpService);
    }
}
```

### Performance Testing

Create performance benchmarks for plugins:

```csharp
[TestClass]
public class PluginPerformanceTests
{
    private CustomSegmentationEngine _engine;
    
    [TestInitialize]
    public void Setup()
    {
        var logger = new Mock<ILogger<CustomSegmentationEngine>>().Object;
        _engine = new CustomSegmentationEngine(logger);
    }
    
    [TestMethod]
    public async Task PerformanceBenchmark_SmallVolume_CompletesWithinTimeLimit()
    {
        // Arrange
        var volume = CreateTestVolume(128, 128, 64);
        var options = new SegmentationOptions("test-model.onnx", 0.5f);
        var maxExecutionTime = TimeSpan.FromSeconds(5);
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _engine.RunAsync(volume, options, CancellationToken.None);
        stopwatch.Stop();
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(stopwatch.Elapsed < maxExecutionTime, 
            $"Execution took {stopwatch.Elapsed.TotalSeconds:F2}s, expected < {maxExecutionTime.TotalSeconds}s");
    }
    
    [TestMethod]
    public async Task MemoryUsageBenchmark_LargeVolume_StaysWithinMemoryLimit()
    {
        // Arrange
        var volume = CreateTestVolume(512, 512, 256);
        var options = new SegmentationOptions("test-model.onnx", 0.5f);
        var maxMemoryIncrease = 2L * 1024 * 1024 * 1024; // 2GB
        
        // Act
        var memoryBefore = GC.GetTotalMemory(true);
        var result = await _engine.RunAsync(volume, options, CancellationToken.None);
        var memoryAfter = GC.GetTotalMemory(false);
        var memoryIncrease = memoryAfter - memoryBefore;
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(memoryIncrease < maxMemoryIncrease,
            $"Memory increased by {memoryIncrease / 1024 / 1024}MB, expected < {maxMemoryIncrease / 1024 / 1024}MB");
    }
}
```

## Deployment and Distribution

### Plugin Packaging

Create NuGet packages for plugin distribution:

```xml
<!-- MyCustomPlugin.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageId>MedicalAI.Plugins.CustomSegmentation</PackageId>
    <PackageVersion>1.0.0</PackageVersion>
    <Authors>Your Name</Authors>
    <Description>Custom segmentation plugin for MedicalAI Thesis Suite</Description>
    <PackageTags>medical-ai;segmentation;plugin</PackageTags>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <RepositoryUrl>https://github.com/yourorg/medical-ai-custom-plugin</RepositoryUrl>
  </PropertyGroup>
  
  <!-- Include ONNX models in package -->
  <ItemGroup>
    <Content Include="models\*.onnx">
      <PackagePath>contentFiles\any\net8.0\models\</PackagePath>
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
</Project>
```

### Plugin Discovery

Implement automatic plugin discovery:

```csharp
public class PluginDiscoveryService
{
    private readonly ILogger<PluginDiscoveryService> _logger;
    
    public PluginDiscoveryService(ILogger<PluginDiscoveryService> logger)
    {
        _logger = logger;
    }
    
    public IEnumerable<PluginInfo> DiscoverPlugins(string pluginDirectory)
    {
        var plugins = new List<PluginInfo>();
        
        if (!Directory.Exists(pluginDirectory))
        {
            _logger.LogWarning("Plugin directory not found: {PluginDirectory}", pluginDirectory);
            return plugins;
        }
        
        var assemblyFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.AllDirectories);
        
        foreach (var assemblyFile in assemblyFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(assemblyFile);
                var pluginTypes = FindPluginTypes(assembly);
                
                foreach (var pluginType in pluginTypes)
                {
                    var pluginInfo = CreatePluginInfo(pluginType, assemblyFile);
                    plugins.Add(pluginInfo);
                    _logger.LogInformation("Discovered plugin: {PluginName} ({PluginType})", 
                        pluginInfo.Name, pluginInfo.Type);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin assembly: {AssemblyFile}", assemblyFile);
            }
        }
        
        return plugins;
    }
    
    private IEnumerable<Type> FindPluginTypes(Assembly assembly)
    {
        var pluginInterfaces = new[]
        {
            typeof(ISegmentationEngine),
            typeof(IClassificationEngine),
            typeof(INlpReasoningService)
        };
        
        return assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => pluginInterfaces.Any(iface => iface.IsAssignableFrom(type)));
    }
    
    private PluginInfo CreatePluginInfo(Type pluginType, string assemblyPath)
    {
        var attribute = pluginType.GetCustomAttribute<PluginAttribute>();
        
        return new PluginInfo
        {
            Name = attribute?.Name ?? pluginType.Name,
            Description = attribute?.Description ?? string.Empty,
            Version = attribute?.Version ?? "1.0.0",
            Type = pluginType,
            AssemblyPath = assemblyPath,
            InterfaceType = GetPluginInterfaceType(pluginType)
        };
    }
    
    private Type GetPluginInterfaceType(Type pluginType)
    {
        if (typeof(ISegmentationEngine).IsAssignableFrom(pluginType))
            return typeof(ISegmentationEngine);
        if (typeof(IClassificationEngine).IsAssignableFrom(pluginType))
            return typeof(IClassificationEngine);
        if (typeof(INlpReasoningService).IsAssignableFrom(pluginType))
            return typeof(INlpReasoningService);
        
        throw new InvalidOperationException($"Unknown plugin interface for type: {pluginType}");
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute : Attribute
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
}

public class PluginInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public Type Type { get; set; } = null!;
    public Type InterfaceType { get; set; } = null!;
    public string AssemblyPath { get; set; } = string.Empty;
}
```

## Best Practices

### Plugin Development Guidelines

1. **Follow Interface Contracts**: Always implement the full interface contract
2. **Handle Errors Gracefully**: Provide meaningful error messages and proper exception handling
3. **Validate Inputs**: Always validate input parameters and provide clear error messages
4. **Use Async Patterns**: Implement proper async/await patterns for I/O operations
5. **Resource Management**: Properly dispose of resources and handle memory management
6. **Logging**: Provide comprehensive logging for debugging and monitoring
7. **Configuration**: Make plugins configurable through dependency injection
8. **Testing**: Write comprehensive unit and integration tests
9. **Documentation**: Provide clear documentation and usage examples
10. **Performance**: Consider performance implications and provide benchmarks

### Security Considerations

1. **Input Validation**: Validate all inputs, especially file paths and model parameters
2. **Model Security**: Verify model integrity and authenticity
3. **Resource Limits**: Implement resource limits to prevent DoS attacks
4. **Sandboxing**: Consider running plugins in isolated environments
5. **Dependency Management**: Keep dependencies up to date and scan for vulnerabilities

### Performance Optimization

1. **Memory Management**: Use memory pools and efficient data structures
2. **Caching**: Implement intelligent caching strategies
3. **Parallel Processing**: Utilize parallel processing where appropriate
4. **GPU Acceleration**: Leverage GPU acceleration when available
5. **Profiling**: Regular performance profiling and optimization

This plugin development guide provides comprehensive information for creating robust, efficient, and maintainable AI model plugins for the MedicalAI Thesis Suite.