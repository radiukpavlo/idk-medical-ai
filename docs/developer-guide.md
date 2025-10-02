# Developer Guide

This comprehensive guide provides developers with everything needed to understand, extend, and contribute to the MedicalAI Thesis Suite.

## Table of Contents
- [Architecture Overview](#architecture-overview)
- [Development Environment Setup](#development-environment-setup)
- [Project Structure](#project-structure)
- [Design Patterns and Principles](#design-patterns-and-principles)
- [Plugin Development](#plugin-development)
- [Testing Strategy](#testing-strategy)
- [Code Style and Standards](#code-style-and-standards)
- [Performance Considerations](#performance-considerations)
- [Security Guidelines](#security-guidelines)
- [Contributing Guidelines](#contributing-guidelines)

## Architecture Overview

### Clean Architecture Implementation

The MedicalAI Thesis Suite implements Clean Architecture principles to ensure maintainability, testability, and separation of concerns.

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │   Avalonia UI   │  │   CLI Interface │  │  Web API    │ │
│  │  (Views, VMs)   │  │   (Commands)    │  │ (Future)    │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────────┐
│                   Application Layer                          │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │    Commands     │  │     Queries     │  │  Handlers   │ │
│  │   (CQRS)        │  │   (Future)      │  │ (MediatR)   │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────────┐
│                  Infrastructure Layer                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │  Data Access    │  │   External      │  │   AI/ML     │ │
│  │ (EF Core, DB)   │  │   Services      │  │  Engines    │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────────┐
│                      Core Layer                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │    Entities     │  │   Interfaces    │  │   Value     │ │
│  │   (Domain)      │  │  (Contracts)    │  │  Objects    │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Dependency Flow

Dependencies flow inward toward the Core layer:
- **Presentation** → **Application** → **Infrastructure** → **Core**
- **Core** has no dependencies on other layers
- **Infrastructure** implements interfaces defined in **Core**
- **Application** orchestrates use cases using **Core** interfaces

### Key Architectural Decisions

#### 1. CQRS with MediatR
- **Commands**: Modify system state (ImportDicom, RunSegmentation)
- **Queries**: Read system state (future implementation)
- **Handlers**: Process commands and queries
- **Benefits**: Separation of concerns, testability, scalability

#### 2. Plugin Architecture
- **Interface-based**: Plugins implement core interfaces
- **Dependency Injection**: Plugins registered in DI container
- **Hot-swappable**: Different implementations can be swapped
- **Extensible**: New AI models can be added as plugins

#### 3. Domain-Driven Design
- **Entities**: Core business objects (Patient, Study, Series)
- **Value Objects**: Immutable data structures (Volume3D, Mask3D)
- **Aggregates**: Consistency boundaries
- **Domain Services**: Business logic that doesn't belong to entities

## Development Environment Setup

### Prerequisites
- **.NET 8.0 SDK**: Latest version
- **IDE**: Visual Studio 2022, JetBrains Rider, or VS Code
- **Git**: Version control
- **Docker**: For containerized development (optional)

### IDE Configuration

#### Visual Studio 2022
```xml
<!-- .editorconfig -->
root = true

[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# C# formatting rules
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
```

#### VS Code Extensions
```json
{
  "recommendations": [
    "ms-dotnettools.csharp",
    "ms-dotnettools.vscode-dotnet-runtime",
    "formulahendry.dotnet-test-explorer",
    "jchannon.csharpextensions",
    "adrianwilczynski.namespace"
  ]
}
```

### Development Workflow

#### 1. Clone and Setup
```bash
git clone <repository-url>
cd MedicalAI.ThesisSuite
dotnet restore
dotnet build
```

#### 2. Run Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/MedicalAI.Core.Tests
```

#### 3. Development Server
```bash
# Run UI application
dotnet run --project src/MedicalAI.UI

# Run CLI application
dotnet run --project src/MedicalAI.CLI

# Watch for changes (hot reload)
dotnet watch --project src/MedicalAI.UI
```

## Project Structure

### Solution Organization
```
MedicalAI.ThesisSuite/
├── src/                          # Source code
│   ├── MedicalAI.Core/           # Domain layer
│   ├── MedicalAI.Application/    # Application layer
│   ├── MedicalAI.Infrastructure/ # Infrastructure layer
│   ├── MedicalAI.UI/            # Presentation layer (Avalonia)
│   ├── MedicalAI.CLI/           # CLI interface
│   └── MedicalAI.Plugins/       # AI model plugins
├── tests/                       # Test projects
├── docs/                        # Documentation
├── datasets/                    # Sample data
├── models/                      # AI model files
├── build/                       # Build scripts
└── .github/                     # CI/CD workflows
```

### Core Layer Structure
```
MedicalAI.Core/
├── Entities/                    # Domain entities
│   ├── Patient.cs
│   ├── Study.cs
│   └── Series.cs
├── ValueObjects/               # Immutable value objects
│   ├── Volume3D.cs
│   ├── Mask3D.cs
│   └── GraphDescriptor.cs
├── Interfaces/                 # Contracts
│   ├── Imaging/
│   │   ├── IDicomImportService.cs
│   │   ├── IDicomAnonymizerService.cs
│   │   └── IVolumeStore.cs
│   └── ML/
│       ├── ISegmentationEngine.cs
│       ├── IClassificationEngine.cs
│       ├── IKnowledgeDistillationService.cs
│       └── INlpReasoningService.cs
├── Exceptions/                 # Domain exceptions
└── Common/                     # Shared utilities
```

### Application Layer Structure
```
MedicalAI.Application/
├── Commands/                   # CQRS commands
│   ├── ImportDicomCommand.cs
│   ├── RunSegmentationCommand.cs
│   ├── RunClassificationCommand.cs
│   └── GenerateNlpSummaryCommand.cs
├── Queries/                    # CQRS queries (future)
├── Handlers/                   # Command/query handlers
├── Validators/                 # Input validation
├── DTOs/                       # Data transfer objects
└── Services/                   # Application services
```

### Infrastructure Layer Structure
```
MedicalAI.Infrastructure/
├── Data/                       # Data access
│   ├── Contexts/
│   ├── Repositories/
│   └── Configurations/
├── Services/                   # External services
│   ├── DicomImportService.cs
│   ├── VolumeStoreService.cs
│   └── FileSystemService.cs
├── ML/                         # AI/ML implementations
│   ├── MockEngines/           # Mock implementations
│   ├── OnnxRunners/           # ONNX Runtime integration
│   └── ModelLoaders/          # Model loading utilities
└── Configuration/             # Infrastructure configuration
```

## Design Patterns and Principles

### SOLID Principles

#### Single Responsibility Principle (SRP)
Each class has one reason to change:
```csharp
// Good: Single responsibility
public class DicomAnonymizer
{
    public Task<int> AnonymizeAsync(IEnumerable<string> files, AnonymizerProfile profile)
    {
        // Only handles DICOM anonymization
    }
}

// Bad: Multiple responsibilities
public class DicomProcessor
{
    public Task ImportAsync(string path) { /* Import logic */ }
    public Task AnonymizeAsync(string path) { /* Anonymization logic */ }
    public Task ConvertAsync(string path) { /* Conversion logic */ }
}
```

#### Open/Closed Principle (OCP)
Open for extension, closed for modification:
```csharp
// Base interface - closed for modification
public interface ISegmentationEngine
{
    Task<SegmentationResult> RunAsync(Volume3D volume, SegmentationOptions options, CancellationToken ct);
}

// Extensions - open for extension
public class SkifSegEngine : ISegmentationEngine { /* Implementation */ }
public class UNetEngine : ISegmentationEngine { /* Implementation */ }
public class CustomEngine : ISegmentationEngine { /* Implementation */ }
```

#### Liskov Substitution Principle (LSP)
Derived classes must be substitutable for base classes:
```csharp
public void ProcessImage(ISegmentationEngine engine, Volume3D volume)
{
    // Any implementation of ISegmentationEngine should work
    var result = await engine.RunAsync(volume, options, cancellationToken);
    // Process result...
}
```

#### Interface Segregation Principle (ISP)
Clients shouldn't depend on interfaces they don't use:
```csharp
// Good: Segregated interfaces
public interface IDicomReader
{
    Task<DicomDataset> ReadAsync(string path);
}

public interface IDicomWriter
{
    Task WriteAsync(DicomDataset dataset, string path);
}

// Bad: Fat interface
public interface IDicomProcessor
{
    Task<DicomDataset> ReadAsync(string path);
    Task WriteAsync(DicomDataset dataset, string path);
    Task AnonymizeAsync(string path);
    Task ConvertAsync(string path);
    Task ValidateAsync(string path);
}
```

#### Dependency Inversion Principle (DIP)
Depend on abstractions, not concretions:
```csharp
// Good: Depends on abstraction
public class SegmentationHandler
{
    private readonly ISegmentationEngine _engine; // Abstraction
    
    public SegmentationHandler(ISegmentationEngine engine)
    {
        _engine = engine;
    }
}

// Bad: Depends on concrete implementation
public class SegmentationHandler
{
    private readonly SkifSegEngine _engine; // Concrete class
}
```

### Repository Pattern
```csharp
public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<Patient>> GetAllAsync(CancellationToken ct);
    Task<Patient> AddAsync(Patient patient, CancellationToken ct);
    Task UpdateAsync(Patient patient, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

public class EfPatientRepository : IPatientRepository
{
    private readonly MedicalAIDbContext _context;
    
    public EfPatientRepository(MedicalAIDbContext context)
    {
        _context = context;
    }
    
    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }
    
    // Other implementations...
}
```

### Factory Pattern
```csharp
public interface ISegmentationEngineFactory
{
    ISegmentationEngine CreateEngine(string engineType);
}

public class SegmentationEngineFactory : ISegmentationEngineFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public SegmentationEngineFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public ISegmentationEngine CreateEngine(string engineType)
    {
        return engineType.ToLower() switch
        {
            "skif-seg" => _serviceProvider.GetRequiredService<SkifSegEngine>(),
            "u-net" => _serviceProvider.GetRequiredService<UNetEngine>(),
            "custom" => _serviceProvider.GetRequiredService<CustomEngine>(),
            _ => throw new ArgumentException($"Unknown engine type: {engineType}")
        };
    }
}
```

### Strategy Pattern
```csharp
public interface IAnonymizationStrategy
{
    Task<DicomDataset> AnonymizeAsync(DicomDataset dataset);
}

public class BasicAnonymizationStrategy : IAnonymizationStrategy
{
    public Task<DicomDataset> AnonymizeAsync(DicomDataset dataset)
    {
        // Remove basic patient identifiers
        dataset.Remove(DicomTag.PatientName);
        dataset.Remove(DicomTag.PatientID);
        return Task.FromResult(dataset);
    }
}

public class EnhancedAnonymizationStrategy : IAnonymizationStrategy
{
    public Task<DicomDataset> AnonymizeAsync(DicomDataset dataset)
    {
        // Remove extended set of identifiers
        // Apply date shifting
        // Remove private tags
        return Task.FromResult(dataset);
    }
}
```

## Plugin Development

### Creating a New Plugin

#### 1. Define Plugin Interface
```csharp
// In MedicalAI.Core
public interface ICustomAnalysisEngine
{
    Task<CustomAnalysisResult> AnalyzeAsync(Volume3D volume, CustomAnalysisOptions options, CancellationToken ct);
}

public record CustomAnalysisOptions(string ModelPath, Dictionary<string, object> Parameters);
public record CustomAnalysisResult(Dictionary<string, float> Metrics, string Summary);
```

#### 2. Create Plugin Project
```xml
<!-- CustomAnalysis.Plugin.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\..\MedicalAI.Core\MedicalAI.Core.csproj" />
    <ProjectReference Include="..\..\MedicalAI.Infrastructure\MedicalAI.Infrastructure.csproj" />
  </ItemGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.ML.OnnxRuntime" Version="1.23.0" />
  </ItemGroup>
</Project>
```

#### 3. Implement Plugin
```csharp
namespace CustomAnalysis.Plugin
{
    public class CustomAnalysisEngine : ICustomAnalysisEngine
    {
        private readonly IOnnxModelRunner _modelRunner;
        private readonly ILogger<CustomAnalysisEngine> _logger;
        
        public CustomAnalysisEngine(IOnnxModelRunner modelRunner, ILogger<CustomAnalysisEngine> logger)
        {
            _modelRunner = modelRunner;
            _logger = logger;
        }
        
        public async Task<CustomAnalysisResult> AnalyzeAsync(Volume3D volume, CustomAnalysisOptions options, CancellationToken ct)
        {
            _logger.LogInformation("Starting custom analysis with model: {ModelPath}", options.ModelPath);
            
            try
            {
                // Preprocess volume
                var input = PreprocessVolume(volume, options.Parameters);
                
                // Run model
                var output = await _modelRunner.RunAsync(options.ModelPath, input, ct);
                
                // Postprocess results
                var metrics = ExtractMetrics(output);
                var summary = GenerateSummary(metrics);
                
                return new CustomAnalysisResult(metrics, summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Custom analysis failed");
                throw;
            }
        }
        
        private float[] PreprocessVolume(Volume3D volume, Dictionary<string, object> parameters)
        {
            // Implementation specific preprocessing
            return new float[volume.Width * volume.Height * volume.Depth];
        }
        
        private Dictionary<string, float> ExtractMetrics(float[] output)
        {
            return new Dictionary<string, float>
            {
                { "confidence", output.Max() },
                { "entropy", CalculateEntropy(output) },
                { "mean_activation", output.Average() }
            };
        }
        
        private string GenerateSummary(Dictionary<string, float> metrics)
        {
            return $"Analysis completed with confidence: {metrics["confidence"]:F2}";
        }
        
        private float CalculateEntropy(float[] values)
        {
            // Shannon entropy calculation
            return values.Where(v => v > 0).Sum(v => -v * MathF.Log2(v));
        }
    }
    
    // Extension method for DI registration
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCustomAnalysis(this IServiceCollection services)
        {
            services.AddSingleton<ICustomAnalysisEngine, CustomAnalysisEngine>();
            return services;
        }
    }
}
```

#### 4. Register Plugin
```csharp
// In Program.cs or Startup.cs
services.AddCustomAnalysis();
```

### Plugin Best Practices

#### 1. Error Handling
```csharp
public async Task<SegmentationResult> RunAsync(Volume3D volume, SegmentationOptions options, CancellationToken ct)
{
    try
    {
        // Validate inputs
        ValidateInputs(volume, options);
        
        // Check model availability
        if (!File.Exists(options.ModelPath))
        {
            throw new FileNotFoundException($"Model file not found: {options.ModelPath}");
        }
        
        // Run processing
        var result = await ProcessAsync(volume, options, ct);
        
        // Validate outputs
        ValidateOutputs(result);
        
        return result;
    }
    catch (OperationCanceledException)
    {
        _logger.LogInformation("Operation was cancelled");
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Segmentation failed for model: {ModelPath}", options.ModelPath);
        throw new ProcessingException("Segmentation processing failed", ex);
    }
}
```

#### 2. Resource Management
```csharp
public class ResourceAwareEngine : ISegmentationEngine, IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentDictionary<string, IOnnxSession> _modelCache;
    private bool _disposed;
    
    public ResourceAwareEngine()
    {
        _semaphore = new SemaphoreSlim(Environment.ProcessorCount);
        _modelCache = new ConcurrentDictionary<string, IOnnxSession>();
    }
    
    public async Task<SegmentationResult> RunAsync(Volume3D volume, SegmentationOptions options, CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var session = _modelCache.GetOrAdd(options.ModelPath, LoadModel);
            return await ProcessWithSession(session, volume, options, ct);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _semaphore?.Dispose();
            foreach (var session in _modelCache.Values)
            {
                session?.Dispose();
            }
            _modelCache.Clear();
            _disposed = true;
        }
    }
}
```

#### 3. Configuration Management
```csharp
public class ConfigurableEngine : ISegmentationEngine
{
    private readonly EngineConfiguration _config;
    
    public ConfigurableEngine(IOptions<EngineConfiguration> config)
    {
        _config = config.Value;
    }
    
    public async Task<SegmentationResult> RunAsync(Volume3D volume, SegmentationOptions options, CancellationToken ct)
    {
        var effectiveOptions = MergeWithDefaults(options, _config);
        return await ProcessAsync(volume, effectiveOptions, ct);
    }
}

public class EngineConfiguration
{
    public float DefaultThreshold { get; set; } = 0.5f;
    public int MaxBatchSize { get; set; } = 4;
    public bool EnableGpuAcceleration { get; set; } = true;
    public Dictionary<string, object> ModelDefaults { get; set; } = new();
}
```

## Testing Strategy

### Test Pyramid

```
    ┌─────────────────┐
    │   E2E Tests     │  ← Few, slow, high confidence
    │   (UI Tests)    │
    ├─────────────────┤
    │ Integration     │  ← Some, medium speed
    │    Tests        │
    ├─────────────────┤
    │   Unit Tests    │  ← Many, fast, focused
    └─────────────────┘
```

### Unit Testing

#### Testing Core Logic
```csharp
[TestClass]
public class SegmentationMetricsTests
{
    [TestMethod]
    public void Dice_WithIdenticalMasks_ReturnsOne()
    {
        // Arrange
        var mask1 = new byte[] { 1, 1, 0, 1, 0 };
        var mask2 = new byte[] { 1, 1, 0, 1, 0 };
        
        // Act
        var dice = SegmentationMetrics.Dice(mask1, mask2);
        
        // Assert
        Assert.AreEqual(1.0, dice, 0.001);
    }
    
    [TestMethod]
    public void Dice_WithNoOverlap_ReturnsZero()
    {
        // Arrange
        var mask1 = new byte[] { 1, 1, 0, 0, 0 };
        var mask2 = new byte[] { 0, 0, 1, 1, 1 };
        
        // Act
        var dice = SegmentationMetrics.Dice(mask1, mask2);
        
        // Assert
        Assert.AreEqual(0.0, dice, 0.001);
    }
}
```

#### Testing Command Handlers
```csharp
[TestClass]
public class ImportDicomHandlerTests
{
    private Mock<IDicomImportService> _mockImportService;
    private ImportDicomHandler _handler;
    
    [TestInitialize]
    public void Setup()
    {
        _mockImportService = new Mock<IDicomImportService>();
        _handler = new ImportDicomHandler(_mockImportService.Object);
    }
    
    [TestMethod]
    public async Task Handle_ValidCommand_CallsImportService()
    {
        // Arrange
        var command = new ImportDicomCommand("/path/to/dicom", true);
        var expectedResult = new ImportResult(1, 2, 10);
        
        _mockImportService
            .Setup(x => x.ImportAsync(It.IsAny<string>(), It.IsAny<DicomImportOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.AreEqual(expectedResult, result);
        _mockImportService.Verify(x => x.ImportAsync(
            "/path/to/dicom", 
            It.Is<DicomImportOptions>(o => o.Anonymize == true), 
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Integration Testing

#### Testing Database Integration
```csharp
[TestClass]
public class PatientRepositoryIntegrationTests
{
    private MedicalAIDbContext _context;
    private PatientRepository _repository;
    
    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<MedicalAIDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new MedicalAIDbContext(options);
        _repository = new PatientRepository(_context);
    }
    
    [TestMethod]
    public async Task AddAsync_ValidPatient_SavesSuccessfully()
    {
        // Arrange
        var patient = new Patient(Guid.NewGuid(), "PATIENT_001");
        
        // Act
        var result = await _repository.AddAsync(patient, CancellationToken.None);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(patient.PseudoId, result.PseudoId);
        
        var savedPatient = await _context.Patients.FindAsync(patient.Id);
        Assert.IsNotNull(savedPatient);
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }
}
```

#### Testing Plugin Integration
```csharp
[TestClass]
public class SegmentationEngineIntegrationTests
{
    private IServiceProvider _serviceProvider;
    
    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSkifSeg();
        _serviceProvider = services.BuildServiceProvider();
    }
    
    [TestMethod]
    public async Task RunAsync_WithValidVolume_ReturnsSegmentationResult()
    {
        // Arrange
        var engine = _serviceProvider.GetRequiredService<ISegmentationEngine>();
        var volume = CreateTestVolume();
        var options = new SegmentationOptions("test-model.onnx", 0.5f);
        
        // Act
        var result = await engine.RunAsync(volume, options, CancellationToken.None);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Mask);
        Assert.IsTrue(result.Labels.Count > 0);
    }
    
    private Volume3D CreateTestVolume()
    {
        var voxels = new byte[64 * 64 * 32];
        return new Volume3D(64, 64, 32, 1.0f, 1.0f, 2.0f, voxels);
    }
}
```

### End-to-End Testing

#### UI Testing with Avalonia
```csharp
[TestClass]
public class MainWindowE2ETests
{
    private TestAppBuilder _appBuilder;
    
    [TestInitialize]
    public void Setup()
    {
        _appBuilder = AvaloniaApp.BuildAvaloniaApp()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
    
    [TestMethod]
    public async Task ImportDicom_ValidFile_ShowsSuccessMessage()
    {
        // Arrange
        using var app = _appBuilder.StartWithClassicDesktopLifetime([]);
        var window = app.GetMainWindow();
        
        // Act
        await window.ClickButton("ImportButton");
        await window.SelectFile("sample.dcm");
        await window.ClickButton("ConfirmButton");
        
        // Assert
        var message = await window.GetText("StatusMessage");
        Assert.IsTrue(message.Contains("Import successful"));
    }
}
```

## Code Style and Standards

### Naming Conventions

#### Classes and Interfaces
```csharp
// Classes: PascalCase
public class DicomImportService { }
public class SegmentationEngine { }

// Interfaces: PascalCase with 'I' prefix
public interface IDicomImportService { }
public interface ISegmentationEngine { }

// Abstract classes: PascalCase with 'Base' suffix (optional)
public abstract class BaseSegmentationEngine { }
```

#### Methods and Properties
```csharp
// Methods: PascalCase, descriptive verbs
public async Task<ImportResult> ImportAsync(string path) { }
public void ValidateInput(Volume3D volume) { }

// Properties: PascalCase, descriptive nouns
public string ModelPath { get; set; }
public float Threshold { get; init; }

// Private fields: camelCase with underscore prefix
private readonly ILogger _logger;
private readonly string _modelPath;
```

#### Constants and Enums
```csharp
// Constants: PascalCase
public const string DefaultModelPath = "models/default.onnx";
public const int MaxBatchSize = 32;

// Enums: PascalCase
public enum ProcessingState
{
    Pending,
    Running,
    Completed,
    Failed
}
```

### Code Organization

#### File Structure
```csharp
// File header (optional)
// Copyright notice, license information

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MedicalAI.Core.Imaging;
using Microsoft.Extensions.Logging;

namespace MedicalAI.Infrastructure.Services
{
    /// <summary>
    /// Provides DICOM file import functionality with anonymization support.
    /// </summary>
    public class DicomImportService : IDicomImportService
    {
        // Private fields
        private readonly ILogger<DicomImportService> _logger;
        private readonly IDicomAnonymizerService _anonymizer;
        
        // Constructor
        public DicomImportService(
            ILogger<DicomImportService> logger,
            IDicomAnonymizerService anonymizer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _anonymizer = anonymizer ?? throw new ArgumentNullException(nameof(anonymizer));
        }
        
        // Public methods
        public async Task<ImportResult> ImportAsync(string path, DicomImportOptions options, CancellationToken ct)
        {
            // Implementation
        }
        
        // Private methods
        private void ValidatePath(string path)
        {
            // Implementation
        }
    }
}
```

#### Documentation Standards
```csharp
/// <summary>
/// Performs medical image segmentation using the SKIF-Seg algorithm.
/// </summary>
/// <param name="volume">The 3D medical image volume to segment.</param>
/// <param name="options">Segmentation configuration options.</param>
/// <param name="ct">Cancellation token for async operation.</param>
/// <returns>
/// A <see cref="SegmentationResult"/> containing the segmentation mask and label mappings.
/// </returns>
/// <exception cref="ArgumentNullException">Thrown when volume or options is null.</exception>
/// <exception cref="FileNotFoundException">Thrown when the model file is not found.</exception>
/// <exception cref="ProcessingException">Thrown when segmentation processing fails.</exception>
public async Task<SegmentationResult> RunAsync(Volume3D volume, SegmentationOptions options, CancellationToken ct)
{
    // Implementation
}
```

### Error Handling Standards

#### Exception Hierarchy
```csharp
// Base application exception
public abstract class MedicalAIException : Exception
{
    protected MedicalAIException(string message) : base(message) { }
    protected MedicalAIException(string message, Exception innerException) : base(message, innerException) { }
}

// Specific exceptions
public class ProcessingException : MedicalAIException
{
    public ProcessingException(string message) : base(message) { }
    public ProcessingException(string message, Exception innerException) : base(message, innerException) { }
}

public class ValidationException : MedicalAIException
{
    public Dictionary<string, string[]> Errors { get; }
    
    public ValidationException(Dictionary<string, string[]> errors) 
        : base("Validation failed")
    {
        Errors = errors;
    }
}
```

#### Error Handling Patterns
```csharp
public async Task<SegmentationResult> RunAsync(Volume3D volume, SegmentationOptions options, CancellationToken ct)
{
    // Input validation
    if (volume == null)
        throw new ArgumentNullException(nameof(volume));
    
    if (options == null)
        throw new ArgumentNullException(nameof(options));
    
    // Business rule validation
    if (volume.Width <= 0 || volume.Height <= 0 || volume.Depth <= 0)
        throw new ValidationException(new Dictionary<string, string[]>
        {
            { nameof(volume), new[] { "Volume dimensions must be positive" } }
        });
    
    try
    {
        _logger.LogInformation("Starting segmentation for volume {Width}x{Height}x{Depth}", 
            volume.Width, volume.Height, volume.Depth);
        
        var result = await ProcessSegmentationAsync(volume, options, ct);
        
        _logger.LogInformation("Segmentation completed successfully");
        return result;
    }
    catch (OperationCanceledException)
    {
        _logger.LogInformation("Segmentation was cancelled");
        throw; // Re-throw cancellation
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Segmentation failed for model: {ModelPath}", options.ModelPath);
        throw new ProcessingException("Segmentation processing failed", ex);
    }
}
```

## Performance Considerations

### Memory Management

#### Large Volume Processing
```csharp
public class MemoryEfficientVolumeProcessor
{
    private readonly MemoryPool<byte> _memoryPool;
    
    public MemoryEfficientVolumeProcessor()
    {
        _memoryPool = MemoryPool<byte>.Shared;
    }
    
    public async Task<SegmentationResult> ProcessLargeVolumeAsync(Volume3D volume, CancellationToken ct)
    {
        // Use memory pool for large allocations
        using var buffer = _memoryPool.Rent(volume.Voxels.Length);
        
        // Process in chunks to manage memory
        var chunkSize = CalculateOptimalChunkSize(volume);
        var results = new List<Mask3D>();
        
        for (int z = 0; z < volume.Depth; z += chunkSize)
        {
            var chunk = ExtractChunk(volume, z, Math.Min(chunkSize, volume.Depth - z));
            var chunkResult = await ProcessChunkAsync(chunk, ct);
            results.Add(chunkResult);
            
            // Force garbage collection if memory pressure is high
            if (GC.GetTotalMemory(false) > GetMemoryThreshold())
            {
                GC.Collect(2, GCCollectionMode.Forced, true);
            }
        }
        
        return CombineResults(results);
    }
    
    private int CalculateOptimalChunkSize(Volume3D volume)
    {
        var availableMemory = GC.GetTotalMemory(false);
        var maxChunkMemory = availableMemory / 4; // Use 25% of available memory
        var voxelsPerSlice = volume.Width * volume.Height;
        return Math.Max(1, (int)(maxChunkMemory / voxelsPerSlice));
    }
}
```

#### Async Patterns
```csharp
public class AsyncBatchProcessor
{
    private readonly SemaphoreSlim _semaphore;
    private readonly int _maxConcurrency;
    
    public AsyncBatchProcessor(int maxConcurrency = Environment.ProcessorCount)
    {
        _maxConcurrency = maxConcurrency;
        _semaphore = new SemaphoreSlim(maxConcurrency);
    }
    
    public async Task<IEnumerable<TResult>> ProcessBatchAsync<TInput, TResult>(
        IEnumerable<TInput> items,
        Func<TInput, CancellationToken, Task<TResult>> processor,
        CancellationToken ct)
    {
        var tasks = items.Select(async item =>
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                return await processor(item, ct);
            }
            finally
            {
                _semaphore.Release();
            }
        });
        
        return await Task.WhenAll(tasks);
    }
}
```

### Caching Strategies

#### Model Caching
```csharp
public class CachedModelRunner : IDisposable
{
    private readonly ConcurrentDictionary<string, (IOnnxSession Session, DateTime LastUsed)> _modelCache;
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _cacheExpiry;
    
    public CachedModelRunner(TimeSpan cacheExpiry)
    {
        _modelCache = new ConcurrentDictionary<string, (IOnnxSession, DateTime)>();
        _cacheExpiry = cacheExpiry;
        _cleanupTimer = new Timer(CleanupExpiredModels, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }
    
    public async Task<float[]> RunModelAsync(string modelPath, float[] input, CancellationToken ct)
    {
        var (session, _) = _modelCache.AddOrUpdate(
            modelPath,
            key => (LoadModel(key), DateTime.UtcNow),
            (key, existing) => (existing.Session, DateTime.UtcNow));
        
        return await session.RunAsync(input, ct);
    }
    
    private void CleanupExpiredModels(object? state)
    {
        var cutoff = DateTime.UtcNow - _cacheExpiry;
        var expiredKeys = _modelCache
            .Where(kvp => kvp.Value.LastUsed < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();
        
        foreach (var key in expiredKeys)
        {
            if (_modelCache.TryRemove(key, out var value))
            {
                value.Session.Dispose();
            }
        }
    }
}
```

## Security Guidelines

### Input Validation

#### File Path Validation
```csharp
public static class PathValidator
{
    private static readonly string[] AllowedExtensions = { ".dcm", ".nii", ".nii.gz" };
    private static readonly char[] InvalidChars = Path.GetInvalidPathChars();
    
    public static void ValidateFilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        
        if (path.IndexOfAny(InvalidChars) >= 0)
            throw new ArgumentException("Path contains invalid characters", nameof(path));
        
        if (Path.IsPathRooted(path) && !IsAllowedDirectory(path))
            throw new SecurityException("Access to this directory is not allowed");
        
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException($"File extension '{extension}' is not allowed", nameof(path));
    }
    
    private static bool IsAllowedDirectory(string path)
    {
        var allowedDirectories = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Path.Combine(Environment.CurrentDirectory, "datasets"),
            Path.Combine(Environment.CurrentDirectory, "models")
        };
        
        return allowedDirectories.Any(allowed => path.StartsWith(allowed, StringComparison.OrdinalIgnoreCase));
    }
}
```

#### Data Sanitization
```csharp
public static class DicomSanitizer
{
    private static readonly DicomTag[] SensitiveTags = 
    {
        DicomTag.PatientName,
        DicomTag.PatientID,
        DicomTag.PatientBirthDate,
        DicomTag.PatientAddress,
        DicomTag.InstitutionName,
        DicomTag.ReferringPhysicianName
    };
    
    public static DicomDataset SanitizeDataset(DicomDataset dataset)
    {
        var sanitized = dataset.Clone();
        
        foreach (var tag in SensitiveTags)
        {
            if (sanitized.Contains(tag))
            {
                sanitized.Remove(tag);
            }
        }
        
        // Remove private tags
        var privateTags = sanitized.Where(item => item.Tag.IsPrivate).ToList();
        foreach (var tag in privateTags)
        {
            sanitized.Remove(tag.Tag);
        }
        
        return sanitized;
    }
}
```

### Secure Configuration

#### Secrets Management
```csharp
public class SecureConfiguration
{
    private readonly IConfiguration _configuration;
    
    public SecureConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public string GetConnectionString(string name)
    {
        // Try environment variable first
        var envVar = $"ConnectionStrings__{name}";
        var connectionString = Environment.GetEnvironmentVariable(envVar);
        
        if (!string.IsNullOrEmpty(connectionString))
            return connectionString;
        
        // Fall back to configuration
        return _configuration.GetConnectionString(name) 
            ?? throw new InvalidOperationException($"Connection string '{name}' not found");
    }
    
    public string GetApiKey(string service)
    {
        var key = _configuration[$"ApiKeys:{service}"];
        if (string.IsNullOrEmpty(key))
        {
            throw new InvalidOperationException($"API key for '{service}' not configured");
        }
        return key;
    }
}
```

## Contributing Guidelines

### Pull Request Process

1. **Fork and Branch**
   ```bash
   git fork <repository-url>
   git checkout -b feature/your-feature-name
   ```

2. **Development**
   - Follow coding standards
   - Add comprehensive tests
   - Update documentation
   - Ensure all tests pass

3. **Commit Messages**
   ```
   feat: add SKIF-Seg segmentation engine
   
   - Implement SKIF-Seg algorithm for cardiac MRI segmentation
   - Add comprehensive unit tests
   - Update API documentation
   - Add performance benchmarks
   
   Closes #123
   ```

4. **Pull Request**
   - Provide clear description
   - Reference related issues
   - Include screenshots for UI changes
   - Ensure CI/CD passes

### Code Review Checklist

#### Functionality
- [ ] Code works as intended
- [ ] Edge cases are handled
- [ ] Error handling is appropriate
- [ ] Performance is acceptable

#### Code Quality
- [ ] Follows coding standards
- [ ] Is well-documented
- [ ] Has appropriate tests
- [ ] No code duplication

#### Security
- [ ] Input validation is present
- [ ] No sensitive data exposure
- [ ] Secure coding practices followed
- [ ] Dependencies are secure

#### Architecture
- [ ] Follows established patterns
- [ ] Maintains separation of concerns
- [ ] Doesn't break existing functionality
- [ ] Is extensible and maintainable

This developer guide provides comprehensive information for understanding, extending, and contributing to the MedicalAI Thesis Suite codebase.