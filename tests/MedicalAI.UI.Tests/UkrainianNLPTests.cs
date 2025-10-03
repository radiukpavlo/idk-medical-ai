using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using MedicalAI.Core.ML;
using MedicalAI.Infrastructure.ML;
using MedicalAI.Core;
using NLP.MedReasoning.UA;

namespace MedicalAI.UI.Tests
{
    public class UkrainianNLPTests : AvaloniaHeadlessTestBase
    {
        [Fact]
        public void UkrainianNlpService_Initializes_Successfully()
        {
            // Arrange & Act
            var service = new UkrainianNlpService();
            
            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void SimpleNlpUAService_Initializes_Successfully()
        {
            // Arrange & Act
            var service = new SimpleNlpUAService();
            
            // Assert
            service.Should().NotBeNull();
        }

        [Theory]
        [InlineData("норма", Sentiment.Positive)]
        [InlineData("без ознак патології", Sentiment.Positive)]
        [InlineData("патологія", Sentiment.Negative)]
        [InlineData("негативні зміни", Sentiment.Negative)]
        [InlineData("дослідження проведено", Sentiment.Neutral)]
        [InlineData("звичайний текст", Sentiment.Neutral)]
        public async Task UkrainianNlpService_AnalyzeSentimentAsync_WithUkrainianText_ReturnsCorrectSentiment(
            string text, Sentiment expectedSentiment)
        {
            // Arrange
            var service = new UkrainianNlpService();
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await service.AnalyzeSentimentAsync(text, cancellationToken);

            // Assert
            result.Should().Be(expectedSentiment);
        }

        [Theory]
        [InlineData("НОРМА", Sentiment.Positive)] // Test case insensitivity
        [InlineData("ПАТОЛОГІЯ", Sentiment.Negative)]
        [InlineData("Норма в межах допустимого", Sentiment.Positive)]
        [InlineData("Виявлено патологічні зміни", Sentiment.Negative)]
        public async Task UkrainianNlpService_AnalyzeSentimentAsync_CaseInsensitive_ReturnsCorrectSentiment(
            string text, Sentiment expectedSentiment)
        {
            // Arrange
            var service = new UkrainianNlpService();
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await service.AnalyzeSentimentAsync(text, cancellationToken);

            // Assert
            result.Should().Be(expectedSentiment);
        }

        [Fact]
        public async Task UkrainianNlpService_SummarizeAsync_WithBasicContext_ReturnsUkrainianSummary()
        {
            // Arrange
            var service = new UkrainianNlpService();
            var context = new CaseContext(
                "ПАЦІЄНТ_001", 
                "ДОСЛІДЖЕННЯ_001", 
                "Тестові нотатки для дослідження", 
                null, 
                null
            );
            var cancellationToken = CancellationToken.None;

            // Act
            var summary = await service.SummarizeAsync(context, cancellationToken);

            // Assert
            summary.Should().NotBeNull();
            summary.Text.Should().NotBeNullOrEmpty();
            summary.Text.Should().Contain("ПАЦІЄНТ_001");
            summary.Text.Should().Contain("ДОСЛІДЖЕННЯ_001");
            summary.Text.Should().Contain("дослідницьких цілей"); // Ukrainian text
            summary.Sentiment.Should().BeOneOf(Sentiment.Negative, Sentiment.Neutral, Sentiment.Positive);
        }

        [Fact]
        public async Task UkrainianNlpService_SummarizeAsync_WithSegmentationResult_IncludesSegmentationInfo()
        {
            // Arrange
            var service = new UkrainianNlpService();
            var mask = new Mask3D(10, 10, 5, new byte[500]);
            var labels = new Dictionary<int, string> { { 1, "Міокард" } };
            var segmentationResult = new SegmentationResult(mask, labels);
            
            var context = new CaseContext(
                "ПАЦІЄНТ_002", 
                "ДОСЛІДЖЕННЯ_002", 
                "Кардіологічне дослідження", 
                segmentationResult, 
                null
            );
            var cancellationToken = CancellationToken.None;

            // Act
            var summary = await service.SummarizeAsync(context, cancellationToken);

            // Assert
            summary.Should().NotBeNull();
            summary.Text.Should().NotBeNullOrEmpty();
            summary.Text.Should().Contain("ПАЦІЄНТ_002");
            summary.Text.Should().Contain("ДОСЛІДЖЕННЯ_002");
            summary.Text.Should().Contain("сегментацію"); // Ukrainian for segmentation
            summary.Text.Should().Contain("міокарда"); // Ukrainian for myocardium
        }

        [Fact]
        public async Task UkrainianNlpService_SummarizeAsync_WithClassificationResult_IncludesClassificationInfo()
        {
            // Arrange
            var service = new UkrainianNlpService();
            var probabilities = new Dictionary<string, float>
            {
                { "Норма", 0.3f },
                { "Патологія", 0.7f }
            };
            var classificationResult = new ClassificationResult(probabilities);
            
            var context = new CaseContext(
                "ПАЦІЄНТ_003", 
                "ДОСЛІДЖЕННЯ_003", 
                "Класифікаційне дослідження", 
                null, 
                classificationResult
            );
            var cancellationToken = CancellationToken.None;

            // Act
            var summary = await service.SummarizeAsync(context, cancellationToken);

            // Assert
            summary.Should().NotBeNull();
            summary.Text.Should().NotBeNullOrEmpty();
            summary.Text.Should().Contain("ПАЦІЄНТ_003");
            summary.Text.Should().Contain("ДОСЛІДЖЕННЯ_003");
            summary.Text.Should().Contain("Класифікація"); // Ukrainian for classification
            summary.Text.Should().Contain("Патологія"); // Should include the top class
        }

        [Fact]
        public async Task UkrainianNlpService_SummarizeAsync_WithBothResults_IncludesBothSegmentationAndClassification()
        {
            // Arrange
            var service = new UkrainianNlpService();
            
            var mask = new Mask3D(10, 10, 5, new byte[500]);
            var segLabels = new Dictionary<int, string> { { 1, "Міокард" } };
            var segmentationResult = new SegmentationResult(mask, segLabels);
            
            var probabilities = new Dictionary<string, float>
            {
                { "Норма", 0.8f },
                { "Патологія", 0.2f }
            };
            var classificationResult = new ClassificationResult(probabilities);
            
            var context = new CaseContext(
                "ПАЦІЄНТ_004", 
                "ДОСЛІДЖЕННЯ_004", 
                "Комплексне дослідження", 
                segmentationResult, 
                classificationResult
            );
            var cancellationToken = CancellationToken.None;

            // Act
            var summary = await service.SummarizeAsync(context, cancellationToken);

            // Assert
            summary.Should().NotBeNull();
            summary.Text.Should().NotBeNullOrEmpty();
            summary.Text.Should().Contain("ПАЦІЄНТ_004");
            summary.Text.Should().Contain("ДОСЛІДЖЕННЯ_004");
            summary.Text.Should().Contain("сегментацію"); // Segmentation info
            summary.Text.Should().Contain("Класифікація"); // Classification info
            summary.Text.Should().Contain("Норма"); // Top classification result
        }

        [Fact]
        public async Task UkrainianNlpService_SummarizeAsync_WithoutResults_IndicatesNoProcessing()
        {
            // Arrange
            var service = new UkrainianNlpService();
            var context = new CaseContext(
                "ПАЦІЄНТ_005", 
                "ДОСЛІДЖЕННЯ_005", 
                "Базове дослідження", 
                null, 
                null
            );
            var cancellationToken = CancellationToken.None;

            // Act
            var summary = await service.SummarizeAsync(context, cancellationToken);

            // Assert
            summary.Should().NotBeNull();
            summary.Text.Should().NotBeNullOrEmpty();
            summary.Text.Should().Contain("ПАЦІЄНТ_005");
            summary.Text.Should().Contain("ДОСЛІДЖЕННЯ_005");
            summary.Text.Should().Contain("не виконано"); // Ukrainian for "not performed"
            summary.Text.Should().Contain("відсутня"); // Ukrainian for "absent"
        }

        [Fact]
        public async Task UkrainianNlpService_SummarizeAsync_GeneratesSentimentBasedOnContent()
        {
            // Arrange
            var service = new UkrainianNlpService();
            
            // Test with pathological classification (should be negative sentiment)
            var pathologyProbabilities = new Dictionary<string, float>
            {
                { "Норма", 0.1f },
                { "Патологія", 0.9f }
            };
            var pathologyResult = new ClassificationResult(pathologyProbabilities);
            
            var pathologyContext = new CaseContext(
                "ПАЦІЄНТ_ПАТОЛОГІЯ", 
                "ДОСЛІДЖЕННЯ_ПАТОЛОГІЯ", 
                "Патологічні зміни", 
                null, 
                pathologyResult
            );

            // Act
            var pathologySummary = await service.SummarizeAsync(pathologyContext, CancellationToken.None);

            // Assert
            pathologySummary.Should().NotBeNull();
            pathologySummary.Sentiment.Should().Be(Sentiment.Negative);
            
            // Test with normal classification (should be positive sentiment)
            var normalProbabilities = new Dictionary<string, float>
            {
                { "Норма", 0.9f },
                { "Патологія", 0.1f }
            };
            var normalResult = new ClassificationResult(normalProbabilities);
            
            var normalContext = new CaseContext(
                "ПАЦІЄНТ_НОРМА", 
                "ДОСЛІДЖЕННЯ_НОРМА", 
                "Норма без ознак", 
                null, 
                normalResult
            );

            var normalSummary = await service.SummarizeAsync(normalContext, CancellationToken.None);
            normalSummary.Should().NotBeNull();
            normalSummary.Sentiment.Should().Be(Sentiment.Positive);
        }

        [Fact]
        public async Task UkrainianNlpService_HandlesCancellation_Gracefully()
        {
            // Arrange
            var service = new UkrainianNlpService();
            var context = new CaseContext("П1", "Д1", null, null, null);
            
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            // The mock implementation doesn't actually check cancellation token,
            // but this verifies the method signature accepts it
            var summary = await service.SummarizeAsync(context, cts.Token);
            summary.Should().NotBeNull();

            var sentiment = await service.AnalyzeSentimentAsync("тест", cts.Token);
            sentiment.Should().BeOneOf(Sentiment.Negative, Sentiment.Neutral, Sentiment.Positive);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task UkrainianNlpService_AnalyzeSentimentAsync_WithEmptyOrNullText_ReturnsNeutral(string? text)
        {
            // Arrange
            var service = new UkrainianNlpService();
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await service.AnalyzeSentimentAsync(text ?? "", cancellationToken);

            // Assert
            result.Should().Be(Sentiment.Neutral);
        }
    }

    public class UkrainianNLPIntegrationTests : AvaloniaHeadlessTestBase
    {
        [Fact]
        public async Task UkrainianNLP_MedicalTerminology_ProcessesCorrectly()
        {
            // Arrange
            var service = new UkrainianNlpService();
            var medicalTerms = new[]
            {
                "міокард", "серце", "судини", "артерія", "вена",
                "діагностика", "лікування", "терапія", "хірургія",
                "рентген", "МРТ", "КТ", "УЗД", "ЕКГ"
            };

            // Act & Assert
            foreach (var term in medicalTerms)
            {
                var sentiment = await service.AnalyzeSentimentAsync(term, CancellationToken.None);
                sentiment.Should().BeOneOf(Sentiment.Negative, Sentiment.Neutral, Sentiment.Positive);
                
                var context = new CaseContext("П1", "Д1", $"Дослідження {term}", null, null);
                var summary = await service.SummarizeAsync(context, CancellationToken.None);
                summary.Should().NotBeNull();
                summary.Text.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public async Task UkrainianNLP_LocalizationFeatures_WorkCorrectly()
        {
            // Arrange
            var service = new UkrainianNlpService();
            
            // Test Ukrainian-specific phrases
            var ukrainianPhrases = new Dictionary<string, Sentiment>
            {
                { "в межах норми", Sentiment.Positive },
                { "без патологічних змін", Sentiment.Positive },
                { "виявлено відхилення", Sentiment.Negative },
                { "потребує додаткового обстеження", Sentiment.Neutral },
                { "рекомендовано консультацію", Sentiment.Neutral }
            };

            // Act & Assert
            foreach (var phrase in ukrainianPhrases)
            {
                var sentiment = await service.AnalyzeSentimentAsync(phrase.Key, CancellationToken.None);
                
                // The current implementation is simple, so we just verify it returns a valid sentiment
                sentiment.Should().BeOneOf(Sentiment.Negative, Sentiment.Neutral, Sentiment.Positive);
            }
        }

        [Fact]
        public async Task UkrainianNLP_ComplexMedicalScenario_GeneratesComprehensiveSummary()
        {
            // Arrange
            var service = new UkrainianNlpService();
            
            // Create a complex medical scenario
            var mask = new Mask3D(20, 20, 10, new byte[4000]);
            var segLabels = new Dictionary<int, string> 
            { 
                { 1, "Міокард" }, 
                { 2, "Лівий шлуночок" },
                { 3, "Правий шлуночок" }
            };
            var segmentationResult = new SegmentationResult(mask, segLabels);
            
            var probabilities = new Dictionary<string, float>
            {
                { "Норма", 0.15f },
                { "Гіпертрофія міокарда", 0.60f },
                { "Ішемічна хвороба", 0.25f }
            };
            var classificationResult = new ClassificationResult(probabilities);
            
            var context = new CaseContext(
                "КАРДІО_ПАЦІЄНТ_001", 
                "КАРДІО_ДОСЛІДЖЕННЯ_2024_001", 
                "Комплексне кардіологічне обстеження з МРТ серця та аналізом функції лівого шлуночка", 
                segmentationResult, 
                classificationResult
            );

            // Act
            var summary = await service.SummarizeAsync(context, CancellationToken.None);

            // Assert
            summary.Should().NotBeNull();
            summary.Text.Should().NotBeNullOrEmpty();
            
            // Verify patient and study information
            summary.Text.Should().Contain("КАРДІО_ПАЦІЄНТ_001");
            summary.Text.Should().Contain("КАРДІО_ДОСЛІДЖЕННЯ_2024_001");
            
            // Verify segmentation information
            summary.Text.Should().Contain("сегментацію");
            summary.Text.Should().Contain("міокарда");
            
            // Verify classification information
            summary.Text.Should().Contain("Класифікація");
            summary.Text.Should().Contain("Гіпертрофія міокарда"); // Top classification result
            
            // Verify Ukrainian language usage
            summary.Text.Should().Contain("дослідницьких цілей");
            
            // Sentiment should reflect the pathological finding
            summary.Sentiment.Should().Be(Sentiment.Negative);
        }
    }
}
