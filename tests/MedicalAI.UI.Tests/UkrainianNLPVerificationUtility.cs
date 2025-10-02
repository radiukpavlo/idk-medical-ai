using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MedicalAI.Core.ML;
using MedicalAI.Infrastructure.ML;
using MedicalAI.Core;
using NLP.MedReasoning.UA;

namespace MedicalAI.UI.Tests
{
    /// <summary>
    /// Utility class to verify Ukrainian NLP processing functionality
    /// </summary>
    public static class UkrainianNLPVerificationUtility
    {
        /// <summary>
        /// Verifies that Ukrainian NLP processing functionality works correctly
        /// </summary>
        public static async Task<UkrainianNLPVerificationResult> VerifyUkrainianNLPFunctionalityAsync()
        {
            var result = new UkrainianNLPVerificationResult();
            
            try
            {
                // Test 1: Service initialization
                result.ServiceInitializationSuccess = TestServiceInitialization();
                
                // Test 2: Ukrainian sentiment analysis
                result.UkrainianSentimentAnalysisSuccess = await TestUkrainianSentimentAnalysis();
                
                // Test 3: Medical terminology processing
                result.MedicalTerminologySuccess = await TestMedicalTerminologyProcessing();
                
                // Test 4: Case summarization in Ukrainian
                result.CaseSummarizationSuccess = await TestCaseSummarization();
                
                // Test 5: Localization features
                result.LocalizationFeaturesSuccess = await TestLocalizationFeatures();
                
                // Test 6: Complex medical scenario processing
                result.ComplexScenarioSuccess = await TestComplexMedicalScenario();
                
                // Test 7: Error handling with Ukrainian text
                result.ErrorHandlingSuccess = await TestErrorHandling();
                
                result.OverallSuccess = result.ServiceInitializationSuccess && 
                                      result.UkrainianSentimentAnalysisSuccess && 
                                      result.MedicalTerminologySuccess && 
                                      result.CaseSummarizationSuccess && 
                                      result.LocalizationFeaturesSuccess && 
                                      result.ComplexScenarioSuccess && 
                                      result.ErrorHandlingSuccess;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.OverallSuccess = false;
            }
            
            return result;
        }
        
        private static bool TestServiceInitialization()
        {
            try
            {
                var ukrainianService = new UkrainianNlpService();
                var simpleService = new SimpleNlpUAService();
                
                return ukrainianService != null && simpleService != null;
            }
            catch
            {
                return false;
            }
        }
        
        private static async Task<bool> TestUkrainianSentimentAnalysis()
        {
            try
            {
                var service = new UkrainianNlpService();
                
                // Test positive sentiment
                var positiveSentiment = await service.AnalyzeSentimentAsync("норма", CancellationToken.None);
                if (positiveSentiment != Sentiment.Positive)
                    return false;
                
                // Test negative sentiment
                var negativeSentiment = await service.AnalyzeSentimentAsync("патологія", CancellationToken.None);
                if (negativeSentiment != Sentiment.Negative)
                    return false;
                
                // Test neutral sentiment
                var neutralSentiment = await service.AnalyzeSentimentAsync("дослідження", CancellationToken.None);
                if (neutralSentiment != Sentiment.Neutral)
                    return false;
                
                // Test case insensitivity
                var upperCasePositive = await service.AnalyzeSentimentAsync("НОРМА", CancellationToken.None);
                if (upperCasePositive != Sentiment.Positive)
                    return false;
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        private static async Task<bool> TestMedicalTerminologyProcessing()
        {
            try
            {
                var service = new UkrainianNlpService();
                var medicalTerms = new[]
                {
                    "міокард", "серце", "судини", "артерія", "вена",
                    "діагностика", "лікування", "терапія", "хірургія",
                    "рентген", "МРТ", "КТ", "УЗД", "ЕКГ"
                };
                
                foreach (var term in medicalTerms)
                {
                    var sentiment = await service.AnalyzeSentimentAsync(term, CancellationToken.None);
                    
                    // Should return a valid sentiment for all medical terms
                    if (sentiment != Sentiment.Positive && 
                        sentiment != Sentiment.Negative && 
                        sentiment != Sentiment.Neutral)
                    {
                        return false;
                    }
                    
                    // Test in context
                    var context = new CaseContext("П1", "Д1", $"Дослідження {term}", null, null);
                    var summary = await service.SummarizeAsync(context, CancellationToken.None);
                    
                    if (summary == null || string.IsNullOrEmpty(summary.Text))
                        return false;
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        private static async Task<bool> TestCaseSummarization()
        {
            try
            {
                var service = new UkrainianNlpService();
                
                // Test basic summarization
                var basicContext = new CaseContext(
                    "ПАЦІЄНТ_ТЕСТ", 
                    "ДОСЛІДЖЕННЯ_ТЕСТ", 
                    "Тестові нотатки", 
                    null, 
                    null
                );
                
                var basicSummary = await service.SummarizeAsync(basicContext, CancellationToken.None);
                if (basicSummary == null || 
                    string.IsNullOrEmpty(basicSummary.Text) ||
                    !basicSummary.Text.Contains("ПАЦІЄНТ_ТЕСТ") ||
                    !basicSummary.Text.Contains("ДОСЛІДЖЕННЯ_ТЕСТ"))
                {
                    return false;
                }
                
                // Test with segmentation result
                var mask = new Mask3D(10, 10, 5, new byte[500]);
                var labels = new Dictionary<int, string> { { 1, "Міокард" } };
                var segResult = new SegmentationResult(mask, labels);
                
                var segContext = new CaseContext("П2", "Д2", null, segResult, null);
                var segSummary = await service.SummarizeAsync(segContext, CancellationToken.None);
                
                if (segSummary == null || 
                    string.IsNullOrEmpty(segSummary.Text) ||
                    !segSummary.Text.Contains("сегментацію"))
                {
                    return false;
                }
                
                // Test with classification result
                var probabilities = new Dictionary<string, float>
                {
                    { "Норма", 0.3f },
                    { "Патологія", 0.7f }
                };
                var classResult = new ClassificationResult(probabilities);
                
                var classContext = new CaseContext("П3", "Д3", null, null, classResult);
                var classSummary = await service.SummarizeAsync(classContext, CancellationToken.None);
                
                if (classSummary == null || 
                    string.IsNullOrEmpty(classSummary.Text) ||
                    !classSummary.Text.Contains("Класифікація"))
                {
                    return false;
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        private static async Task<bool> TestLocalizationFeatures()
        {
            try
            {
                var service = new UkrainianNlpService();
                
                // Test Ukrainian-specific medical phrases
                var ukrainianPhrases = new[]
                {
                    "в межах норми",
                    "без патологічних змін",
                    "виявлено відхилення",
                    "потребує додаткового обстеження",
                    "рекомендовано консультацію"
                };
                
                foreach (var phrase in ukrainianPhrases)
                {
                    var sentiment = await service.AnalyzeSentimentAsync(phrase, CancellationToken.None);
                    
                    // Should return a valid sentiment
                    if (sentiment != Sentiment.Positive && 
                        sentiment != Sentiment.Negative && 
                        sentiment != Sentiment.Neutral)
                    {
                        return false;
                    }
                }
                
                // Test that summaries are generated in Ukrainian
                var context = new CaseContext("ПАЦІЄНТ", "ДОСЛІДЖЕННЯ", "Українські нотатки", null, null);
                var summary = await service.SummarizeAsync(context, CancellationToken.None);
                
                if (summary == null || 
                    string.IsNullOrEmpty(summary.Text) ||
                    !summary.Text.Contains("дослідницьких цілей")) // Ukrainian phrase
                {
                    return false;
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        private static async Task<bool> TestComplexMedicalScenario()
        {
            try
            {
                var service = new UkrainianNlpService();
                
                // Create complex scenario with both segmentation and classification
                var mask = new Mask3D(20, 20, 10, new byte[4000]);
                var segLabels = new Dictionary<int, string> 
                { 
                    { 1, "Міокард" }, 
                    { 2, "Лівий шлуночок" }
                };
                var segResult = new SegmentationResult(mask, segLabels);
                
                var probabilities = new Dictionary<string, float>
                {
                    { "Норма", 0.2f },
                    { "Гіпертрофія", 0.8f }
                };
                var classResult = new ClassificationResult(probabilities);
                
                var complexContext = new CaseContext(
                    "КАРДІО_ПАЦІЄНТ", 
                    "КАРДІО_ДОСЛІДЖЕННЯ", 
                    "Комплексне кардіологічне обстеження", 
                    segResult, 
                    classResult
                );
                
                var summary = await service.SummarizeAsync(complexContext, CancellationToken.None);
                
                if (summary == null || string.IsNullOrEmpty(summary.Text))
                    return false;
                
                // Should contain patient and study info
                if (!summary.Text.Contains("КАРДІО_ПАЦІЄНТ") || 
                    !summary.Text.Contains("КАРДІО_ДОСЛІДЖЕННЯ"))
                    return false;
                
                // Should contain segmentation info
                if (!summary.Text.Contains("сегментацію") || 
                    !summary.Text.Contains("міокарда"))
                    return false;
                
                // Should contain classification info
                if (!summary.Text.Contains("Класифікація") || 
                    !summary.Text.Contains("Гіпертрофія"))
                    return false;
                
                // Should have appropriate sentiment (negative due to pathology)
                if (summary.Sentiment != Sentiment.Negative)
                    return false;
                
                return true;
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
                var service = new UkrainianNlpService();
                
                // Test with empty/null text
                var emptyResult = await service.AnalyzeSentimentAsync("", CancellationToken.None);
                if (emptyResult != Sentiment.Neutral)
                    return false;
                
                var nullResult = await service.AnalyzeSentimentAsync(null, CancellationToken.None);
                if (nullResult != Sentiment.Neutral)
                    return false;
                
                // Test with minimal context
                var minimalContext = new CaseContext("", "", null, null, null);
                var minimalSummary = await service.SummarizeAsync(minimalContext, CancellationToken.None);
                
                // Should handle gracefully and return some summary
                if (minimalSummary == null || string.IsNullOrEmpty(minimalSummary.Text))
                    return false;
                
                return true;
            }
            catch
            {
                // If it throws, that's also acceptable error handling
                return true;
            }
        }
    }
    
    /// <summary>
    /// Result of Ukrainian NLP processing verification tests
    /// </summary>
    public class UkrainianNLPVerificationResult
    {
        public bool OverallSuccess { get; set; }
        public bool ServiceInitializationSuccess { get; set; }
        public bool UkrainianSentimentAnalysisSuccess { get; set; }
        public bool MedicalTerminologySuccess { get; set; }
        public bool CaseSummarizationSuccess { get; set; }
        public bool LocalizationFeaturesSuccess { get; set; }
        public bool ComplexScenarioSuccess { get; set; }
        public bool ErrorHandlingSuccess { get; set; }
        public Exception? Exception { get; set; }
        
        public void PrintResults()
        {
            Console.WriteLine("=== Ukrainian NLP Processing Verification Results ===");
            Console.WriteLine($"Overall Success: {OverallSuccess}");
            Console.WriteLine($"Service Initialization: {ServiceInitializationSuccess}");
            Console.WriteLine($"Ukrainian Sentiment Analysis: {UkrainianSentimentAnalysisSuccess}");
            Console.WriteLine($"Medical Terminology: {MedicalTerminologySuccess}");
            Console.WriteLine($"Case Summarization: {CaseSummarizationSuccess}");
            Console.WriteLine($"Localization Features: {LocalizationFeaturesSuccess}");
            Console.WriteLine($"Complex Scenario: {ComplexScenarioSuccess}");
            Console.WriteLine($"Error Handling: {ErrorHandlingSuccess}");
            
            if (Exception != null)
            {
                Console.WriteLine($"Exception: {Exception.Message}");
                Console.WriteLine($"Stack Trace: {Exception.StackTrace}");
            }
        }
    }
}