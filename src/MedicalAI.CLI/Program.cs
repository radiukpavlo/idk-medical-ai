using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MedicalAI.Infrastructure.DI;
using MedicalAI.Core;
using MedicalAI.Core.Imaging;
using MedicalAI.Core.ML;
using MedicalAI.Core.Reports;
using Serilog;

namespace MedicalAI.CLI
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            var inputPath = "datasets/samples/sample.nii";
            var modelPath = "models/segmentation/mock.onnx";
            var outputPath = "CaseReport.pdf";

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--input":
                        if (i + 1 < args.Length) inputPath = args[++i];
                        break;
                    case "--model":
                        if (i + 1 < args.Length) modelPath = args[++i];
                        break;
                    case "--output":
                        if (i + 1 < args.Length) outputPath = args[++i];
                        break;
                }
            }

            try
            {
                Log.Information("Starting MedicalAI CLI process...");
                Log.Information("Input path: {InputPath}", inputPath);
                Log.Information("Model path: {ModelPath}", modelPath);
                Log.Information("Output path: {OutputPath}", outputPath);

                var sc = new ServiceCollection().AddInfrastructure();
                sc.AddLogging(builder => builder.AddSerilog());
                var sp = sc.BuildServiceProvider();

                var store = sp.GetRequiredService<IVolumeStore>();
                var seg = sp.GetRequiredService<ISegmentationEngine>();
                var nlp = sp.GetRequiredService<INlpReasoningService>();
                var reporter = sp.GetRequiredService<IReportService>();

                Log.Information("Loading volume...");
                var vol = await store.LoadAsync(new ImageRef("MR", inputPath, null, null), default);

                Log.Information("Running segmentation...");
                var res = await seg.RunAsync(vol, new SegmentationOptions(modelPath, 0.5f), default);

                var ctx = new CaseContext("PSEUDO-001", "STUDY-001", null, res, null);

                Log.Information("Summarizing case...");
                var sum = await nlp.SummarizeAsync(ctx, default);

                Log.Information("Generating report...");
                reporter.ExportCaseReport(outputPath, sum);

                Log.Information("Done. Report created at {OutputPath}", outputPath);
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An unhandled error occurred.");
                return 1;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }
    }
}