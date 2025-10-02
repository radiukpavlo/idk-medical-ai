using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MedicalAI.Core.ML;
using MedicalAI.Infrastructure.ML;
using MedicalAI.Core;
using Classification.KIGCN;
using Segmentation.SKIFSeg;
using Distillation.MultiTeacher;
using NLP.MedReasoning.UA;

namespace MedicalAI.UI.Tests
{
    /// <summary>
    /// Utility class to verify AI model integration and inference functionality
    /// </summary>
    public static class AIModelVerificationUtility
    {
        /// <summary>
        /// Verifies that AI model integration and inference functionality works correctly
        /// </summary>
        public static async Task<AIModelVerificationResult> VerifyAIModelFunctionalityAsync()
        {
            var result = new AIModelVerificationResult();
            
            try
            {
                // Test 1: Plugin initialization
                result.PluginInitializationSuccess = TestPluginInitialization();
                
                // Test 2: Classification engine functionality
                result.ClassificationEngineSuccess = await TestClassificationEngine();
                
                // Test 3: Segmentation engine functionality
                result.SegmentationEngineSuccess = await TestSegmentationEngine();
                
                // Test 4: Knowledge distillation service
                result.KnowledgeDistillationSuccess = await TestKnowledgeDistillation();
                
                // Test 5: NLP reasoning service
                result.NlpReasoningSuccess = await TestNlpReasoning();
                
                // Test 6: End-to-end AI workflow
                result.EndToEndAIWorkflowSuccess = await TestEndToEndAIWorkflow();
                
                // Test 7: Error handling and robustness
                result.ErrorHandlingSuccess = await TestErrorHandling();
                
                result.OverallSuccess = result.PluginInitializationSuccess && 
                                      result.ClassificationEngineSuccess && 
                                      result.SegmentationEngineSuccess && 
                                      result.KnowledgeDistillationSuccess && 
                                      result.NlpReasoningSuccess && 
                                      result.EndToEndAIWorkflowSuccess && 
                                      result.ErrorHandlingSuccess;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.OverallSuccess = false;
            }
            
            return result;
        }
        
        private static bool TestPluginInitialization()
        {
            try
            {
                var kigcnEngine = new KigcnEngine();
                var skifSegEngine = new SkifSegEngine();
                var distillationService = new DistillationService();
                var nlpService = new UkrainianNlpService();
                
                return kigcnEngine != null && 
                       skifSegEngine != null && 
                       distillationService != null && 
                       nlpService != null;
            }
            catch
            {
                return false;
            }
        }
        
        private static async Task<bool> TestClassificationEngine()
        {
            try
            {
                var engine = new KigcnEngine();
                var nodes = new List<GraphNode>
                {
                    new GraphNode(1, new float[] { 0.1f, 0.2f, 0.3f }),
                    new GraphNode(2, new float[] { 0.4f, 0.5f, 0.6f })
                };
                var edges = new List<GraphEdge>
                {
                    new GraphEdge(1, 2)
                };
                var graph = new GraphDescriptor(nodes, edges);
                
                var result = await engine.PredictAsync(graph, CancellationToken.None);
                
                return result != null && 
                       result.Probabilities != null && 
                       result.Probabilities.ContainsKey("Normal") && 
                       result.Probabilities.ContainsKey("Pathology") &&
                       result.Probabilities["Normal"] >= 0 && 
                       result.Probabilities["Normal"] <= 1 &&
                       result.Probabilities["Pathology"] >= 0 && 
                       result.Probabilities["Pathology"] <= 1;
            }
            catch
            {
                return false;
            }
        }
        
        private static async Task<bool> TestSegmentationEngine()
        {
            try
            {
                var engine = new SkifSegEngine();
                var volume = new Volume3D(10, 10, 5, 1.0f, 1.0f, 1.0f, new byte[500]);
                var options = new SegmentationOptions("test_model.onnx", 0.5f);
                
                var result = await engine.RunAsync(volume, options, CancellationToken.None);
                
                return result != null && 
                       result.Mask != null && 
                       result.Labels != null && 
                       result.Mask.Width == volume.Width && 
                       result.Mask.Height == volume.Height && 
                       result.Mask.Depth == volume.Depth &&
                       result.Labels.ContainsKey(1);
            }
            catch
            {
                return false;
            }
        }
        
        private static async Task<bool> TestKnowledgeDistillation()
        {
            try
            {
                var service = new DistillationService();
                var config = new DistillationConfig(
                    "test_dataset", 
                    new[] { "teacher1.onnx", "teacher2.onnx" }, 
                    "student.onnx"
                );
                
                var runId = await service.StartAsync(config, CancellationToken.None);
                if (runId == null || string.IsNullOrEmpty(runId.Value))
                    return false;
                
                var status = await service.GetStatusAsync(runId, CancellationToken.None);
                if (status == null)
                    return false;
                
                var modelInfo = await service.GetStudentModelAsync(runId, CancellationToken.None);
                return modelInfo != null && 
                       !string.IsNullOrEmpty(modelInfo.Path) && 
                       !string.IsNullOrEmpty(modelInfo.Sha256);
            }
            catch
            {
                return false;
            }
        }
        
        private static async Task<bool> TestNlpReasoning()
        {
            try
            {
                var service = new UkrainianNlpService();
                
                // Test summarization
                var context = new CaseContext(
                    "TEST_PATIENT", 
                    "TEST_STUDY", 
                    "Тестові нотатки", 
                    null, 
                    null
                );
                var summary = await service.SummarizeAsync(context, CancellationToken.None);
                
                if (summary == null || string.IsNullOrEmpty(summary.Text))
                    return false;
                
                // Test sentiment analysis
                var positiveSentiment = await service.AnalyzeSentimentAsync("норма", CancellationToken.None);
                var negativeSentiment = await service.AnalyzeSentimentAsync("патологія", CancellationToken.None);
                
                return positiveSentiment == Sentiment.Positive && 
                       negativeSentiment == Sentiment.Negative;
            }
            catch
            {
                return false;
            }
        }
        
        private static async Task<bool> TestEndToEndAIWorkflow()
        {
            try
            {
                // Initialize all AI components
                var segmentationEngine = new SkifSegEngine();
                var classificationEngine = new KigcnEngine();
                var nlpService = new UkrainianNlpService();
                
                // Step 1: Create test volume and run segmentation
                var volume = new Volume3D(8, 8, 4, 1.0f, 1.0f, 1.0f, new byte[256]);
                var segOptions = new SegmentationOptions("model.onnx", 0.5f);
                var segResult = await segmentationEngine.RunAsync(volume, segOptions, CancellationToken.None);
                
                if (segResult == null || segResult.Mask == null)
                    return false;
                
                // Step 2: Create test graph and run classification
                var nodes = new List<GraphNode>
                {
                    new GraphNode(1, new float[] { 0.2f, 0.4f, 0.6f })
                };
                var edges = new List<GraphEdge>();
                var graph = new GraphDescriptor(nodes, edges);
                var classResult = await classificationEngine.PredictAsync(graph, CancellationToken.None);
                
                if (classResult == null || classResult.Probabilities == null)
                    return false;
                
                // Step 3: Generate NLP summary with results
                var context = new CaseContext(
                    "WORKFLOW_TEST", 
                    "STUDY_001", 
                    "Комплексне дослідження", 
                    segResult, 
                    classResult
                );
                var summary = await nlpService.SummarizeAsync(context, CancellationToken.None);
                
                return summary != null && 
                       !string.IsNullOrEmpty(summary.Text) && 
                       summary.Text.Contains("WORKFLOW_TEST");
            }
            catch
            {
                return false;
            }
        }
        
        private static async Task<bool> TestErrorHandling()
        {
            try
            {
                // Test with empty/null inputs
                var classificationEngine = new KigcnEngine();
                var emptyGraph = new GraphDescriptor(new List<GraphNode>(), new List<GraphEdge>());
                
                var result = await classificationEngine.PredictAsync(emptyGraph, CancellationToken.None);
                
                // Should handle empty graph gracefully
                return result != null;
            }
            catch
            {
                // If it throws, that's also acceptable error handling
                return true;
            }
        }
    }
    
    /// <summary>
    /// Result of AI model integration and inference verification tests
    /// </summary>
    public class AIModelVerificationResult
    {
        public bool OverallSuccess { get; set; }
        public bool PluginInitializationSuccess { get; set; }
        public bool ClassificationEngineSuccess { get; set; }
        public bool SegmentationEngineSuccess { get; set; }
        public bool KnowledgeDistillationSuccess { get; set; }
        public bool NlpReasoningSuccess { get; set; }
        public bool EndToEndAIWorkflowSuccess { get; set; }
        public bool ErrorHandlingSuccess { get; set; }
        public Exception? Exception { get; set; }
        
        public void PrintResults()
        {
            Console.WriteLine("=== AI Model Integration and Inference Verification Results ===");
            Console.WriteLine($"Overall Success: {OverallSuccess}");
            Console.WriteLine($"Plugin Initialization: {PluginInitializationSuccess}");
            Console.WriteLine($"Classification Engine: {ClassificationEngineSuccess}");
            Console.WriteLine($"Segmentation Engine: {SegmentationEngineSuccess}");
            Console.WriteLine($"Knowledge Distillation: {KnowledgeDistillationSuccess}");
            Console.WriteLine($"NLP Reasoning: {NlpReasoningSuccess}");
            Console.WriteLine($"End-to-End AI Workflow: {EndToEndAIWorkflowSuccess}");
            Console.WriteLine($"Error Handling: {ErrorHandlingSuccess}");
            
            if (Exception != null)
            {
                Console.WriteLine($"Exception: {Exception.Message}");
                Console.WriteLine($"Stack Trace: {Exception.StackTrace}");
            }
        }
    }
}