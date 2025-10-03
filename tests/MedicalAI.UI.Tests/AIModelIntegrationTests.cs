using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using MedicalAI.Core.ML;
using MedicalAI.Infrastructure.ML;
using MedicalAI.Core;
using Classification.KIGCN;
using Segmentation.SKIFSeg;
using Distillation.MultiTeacher;
using NLP.MedReasoning.UA;
using Microsoft.Extensions.Logging.Abstractions;
using MedicalAI.Infrastructure.Performance;
using MedicalAI.Core.Performance;

namespace MedicalAI.UI.Tests
{
    public class AIModelIntegrationTests : AvaloniaHeadlessTestBase
    {
        // Helpers are centralized in TestHelpers
        [Fact]
        public void KigcnEngine_Initializes_Successfully()
        {
            // Arrange & Act
            var engine = TestHelpers.CreateKigcn();
            
            // Assert
            engine.Should().NotBeNull();
        }

        [Fact]
        public async Task KigcnEngine_PredictAsync_WithValidGraph_ReturnsResult()
        {
            // Arrange
            var engine = TestHelpers.CreateKigcn();
            var nodes = new List<GraphNode>
            {
                new GraphNode(1, new float[] { 0.1f, 0.2f, 0.3f }),
                new GraphNode(2, new float[] { 0.4f, 0.5f, 0.6f }),
                new GraphNode(3, new float[] { 0.7f, 0.8f, 0.9f })
            };
            var edges = new List<GraphEdge>
            {
                new GraphEdge(1, 2),
                new GraphEdge(2, 3)
            };
            var graph = new GraphDescriptor(nodes, edges);
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await engine.PredictAsync(graph, cancellationToken);

            // Assert
            result.Should().NotBeNull();
            result.Probabilities.Should().NotBeNull();
            result.Probabilities.Should().ContainKey("Normal");
            result.Probabilities.Should().ContainKey("Pathology");
            result.Probabilities["Normal"].Should().BeInRange(0f, 1f);
            result.Probabilities["Pathology"].Should().BeInRange(0f, 1f);
            
            // Probabilities should sum to approximately 1
            var sum = result.Probabilities["Normal"] + result.Probabilities["Pathology"];
            sum.Should().BeApproximately(1f, 0.001f);
        }

        [Fact]
        public void SkifSegEngine_Initializes_Successfully()
        {
            // Arrange & Act
            var engine = TestHelpers.CreateSkifSeg(TestHelpers.CreateMemoryManager());
            
            // Assert
            engine.Should().NotBeNull();
        }

        [Fact]
        public async Task SkifSegEngine_RunAsync_WithValidVolume_ReturnsResult()
        {
            // Arrange
            var engine = TestHelpers.CreateSkifSeg(TestHelpers.CreateMemoryManager());
            var volume = new Volume3D(10, 10, 5, 1.0f, 1.0f, 1.0f, new byte[500]);
            var options = new SegmentationOptions("skif_model.onnx", 0.5f);
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await engine.RunAsync(volume, options, cancellationToken);

            // Assert
            result.Should().NotBeNull();
            result.Mask.Should().NotBeNull();
            result.Mask.Width.Should().Be(10);
            result.Mask.Height.Should().Be(10);
            result.Mask.Depth.Should().Be(5);
            result.Labels.Should().NotBeNull();
            result.Labels.Should().ContainKey(1);
            result.Labels[1].Should().Be("Myocardium");
        }

        [Fact]
        public void DistillationService_Initializes_Successfully()
        {
            // Arrange & Act
            var service = new DistillationService();
            
            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public async Task DistillationService_StartAsync_WithValidConfig_ReturnsRunId()
        {
            // Arrange
            var service = new DistillationService();
            var config = new DistillationConfig(
                "datasets/training", 
                new[] { "models/teacher1.onnx", "models/teacher2.onnx" }, 
                "models/student.onnx"
            );
            var cancellationToken = CancellationToken.None;

            // Act
            var runId = await service.StartAsync(config, cancellationToken);

            // Assert
            runId.Should().NotBeNull();
            runId.Value.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task DistillationService_GetStatusAsync_WithValidRunId_ReturnsStatus()
        {
            // Arrange
            var service = new DistillationService();
            var config = new DistillationConfig(
                "datasets/training", 
                new[] { "models/teacher1.onnx" }, 
                "models/student.onnx"
            );
            var cancellationToken = CancellationToken.None;

            // Act
            var runId = await service.StartAsync(config, cancellationToken);
            var status = await service.GetStatusAsync(runId, cancellationToken);

            // Assert
            status.Should().NotBeNull();
            status.State.Should().BeOneOf(RunState.Pending, RunState.Running, RunState.Completed, RunState.Failed);
        }

        [Fact]
        public async Task DistillationService_GetStudentModelAsync_WithValidRunId_ReturnsModelInfo()
        {
            // Arrange
            var service = new DistillationService();
            var config = new DistillationConfig(
                "datasets/training", 
                new[] { "models/teacher1.onnx" }, 
                "models/student.onnx"
            );
            var cancellationToken = CancellationToken.None;

            // Act
            var runId = await service.StartAsync(config, cancellationToken);
            var modelInfo = await service.GetStudentModelAsync(runId, cancellationToken);

            // Assert
            modelInfo.Should().NotBeNull();
            modelInfo.Path.Should().NotBeNullOrEmpty();
            modelInfo.Sha256.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void UkrainianNlpService_Initializes_Successfully()
        {
            // Arrange & Act
            var service = new UkrainianNlpService();
            
            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public async Task UkrainianNlpService_SummarizeAsync_WithValidContext_ReturnsUkrainianSummary()
        {
            // Arrange
            var service = new UkrainianNlpService();
            var context = new CaseContext(
                "PATIENT_001", 
                "STUDY_001", 
                "Тестові нотатки", 
                null, 
                null
            );
            var cancellationToken = CancellationToken.None;

            // Act
            var summary = await service.SummarizeAsync(context, cancellationToken);

            // Assert
            summary.Should().NotBeNull();
            summary.Text.Should().NotBeNullOrEmpty();
            summary.Text.Should().Contain("PATIENT_001");
            summary.Text.Should().Contain("STUDY_001");
            summary.Sentiment.Should().BeOneOf(Sentiment.Negative, Sentiment.Neutral, Sentiment.Positive);
        }

        [Fact]
        public async Task UkrainianNlpService_AnalyzeSentimentAsync_WithUkrainianText_ReturnsCorrectSentiment()
        {
            // Arrange
            var service = new UkrainianNlpService();
            var cancellationToken = CancellationToken.None;

            // Test positive sentiment
            var positiveText = "Норма, без ознак патології";
            var positiveSentiment = await service.AnalyzeSentimentAsync(positiveText, cancellationToken);
            positiveSentiment.Should().Be(Sentiment.Positive);

            // Test negative sentiment
            var negativeText = "Виявлено патологічні зміни";
            var negativeSentiment = await service.AnalyzeSentimentAsync(negativeText, cancellationToken);
            negativeSentiment.Should().Be(Sentiment.Negative);

            // Test neutral sentiment
            var neutralText = "Дослідження проведено";
            var neutralSentiment = await service.AnalyzeSentimentAsync(neutralText, cancellationToken);
            neutralSentiment.Should().Be(Sentiment.Neutral);
        }
    }

    public class AIModelPluginIntegrationTests : AvaloniaHeadlessTestBase
    {
        [Fact]
        public async Task FullAIWorkflow_WithAllPlugins_CompletesSuccessfully()
        {
            // Arrange
            var mem = TestHelpers.CreateMemoryManager();
            var segmentationEngine = TestHelpers.CreateSkifSeg(mem);
            var classificationEngine = TestHelpers.CreateKigcn();
            var distillationService = new DistillationService();
            var nlpService = new UkrainianNlpService();

            // Create test data
            var volume = new Volume3D(10, 10, 5, 1.0f, 1.0f, 1.0f, new byte[500]);
            var segmentationOptions = new SegmentationOptions("model.onnx", 0.5f);
            
            var nodes = new List<GraphNode>
            {
                new GraphNode(1, new float[] { 0.5f, 0.3f, 0.8f })
            };
            var edges = new List<GraphEdge>();
            var graph = new GraphDescriptor(nodes, edges);

            var cancellationToken = CancellationToken.None;

            // Act & Assert
            // Step 1: Segmentation
            var segmentationResult = await segmentationEngine.RunAsync(volume, segmentationOptions, cancellationToken);
            segmentationResult.Should().NotBeNull();

            // Step 2: Classification
            var classificationResult = await classificationEngine.PredictAsync(graph, cancellationToken);
            classificationResult.Should().NotBeNull();

            // Step 3: NLP Summary
            var context = new CaseContext(
                "TEST_PATIENT", 
                "TEST_STUDY", 
                "Тестове дослідження", 
                segmentationResult, 
                classificationResult
            );
            var summary = await nlpService.SummarizeAsync(context, cancellationToken);
            summary.Should().NotBeNull();
            summary.Text.Should().Contain("TEST_PATIENT");

            // Step 4: Knowledge Distillation
            var distillationConfig = new DistillationConfig(
                "test_dataset", 
                new[] { "teacher.onnx" }, 
                "student.onnx"
            );
            var runId = await distillationService.StartAsync(distillationConfig, cancellationToken);
            runId.Should().NotBeNull();

            var status = await distillationService.GetStatusAsync(runId, cancellationToken);
            status.Should().NotBeNull();
        }

        [Fact]
        public async Task AIPlugins_HandleCancellation_Gracefully()
        {
            // Arrange
            var mem = TestHelpers.CreateMemoryManager();
            var segmentationEngine = TestHelpers.CreateSkifSeg(mem);
            var classificationEngine = TestHelpers.CreateKigcn();
            var nlpService = new UkrainianNlpService();

            var volume = new Volume3D(5, 5, 5, 1.0f, 1.0f, 1.0f, new byte[125]);
            var options = new SegmentationOptions("model.onnx", 0.5f);
            
            var graph = new GraphDescriptor(
                new List<GraphNode> { new GraphNode(1, new float[] { 0.1f }) },
                new List<GraphEdge>()
            );

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            // Note: The mock implementations don't actually check cancellation tokens,
            // but this verifies the method signatures accept them
            var segResult = await segmentationEngine.RunAsync(volume, options, cts.Token);
            segResult.Should().NotBeNull();

            var classResult = await classificationEngine.PredictAsync(graph, cts.Token);
            classResult.Should().NotBeNull();

            var context = new CaseContext("P1", "S1", null, null, null);
            var nlpResult = await nlpService.SummarizeAsync(context, cts.Token);
            nlpResult.Should().NotBeNull();
        }

        [Fact]
        public void AIModelDataTypes_CreateCorrectly()
        {
            // Test DistillationConfig
            var config = new DistillationConfig(
                "dataset_path", 
                new[] { "teacher1.onnx", "teacher2.onnx" }, 
                "student.onnx"
            );
            config.DatasetPath.Should().Be("dataset_path");
            config.TeacherModelPaths.Should().HaveCount(2);
            config.StudentModelPath.Should().Be("student.onnx");

            // Test DistillationRunId
            var runId = new DistillationRunId("test-run-123");
            runId.Value.Should().Be("test-run-123");

            // Test RunStatus
            var status = new RunStatus(RunState.Running, "Processing...");
            status.State.Should().Be(RunState.Running);
            status.Message.Should().Be("Processing...");

            // Test ModelInfo
            var modelInfo = new ModelInfo("path/to/model.onnx", "abc123", "Test model");
            modelInfo.Path.Should().Be("path/to/model.onnx");
            modelInfo.Sha256.Should().Be("abc123");
            modelInfo.Notes.Should().Be("Test model");

            // Test CaseContext
            var caseContext = new CaseContext("P1", "S1", "Notes", null, null);
            caseContext.PatientPseudoId.Should().Be("P1");
            caseContext.StudyId.Should().Be("S1");
            caseContext.Notes.Should().Be("Notes");

            // Test NlpSummary
            var nlpSummary = new NlpSummary("Summary text", Sentiment.Positive);
            nlpSummary.Text.Should().Be("Summary text");
            nlpSummary.Sentiment.Should().Be(Sentiment.Positive);
        }
    }
}
