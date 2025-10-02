using System;
using Microsoft.Extensions.DependencyInjection;
using MedicalAI.Infrastructure.DI;
using MedicalAI.Infrastructure.Imaging;
using MedicalAI.Core;
using MedicalAI.Core.ML;

namespace MedicalAI.CLI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var sc = new ServiceCollection().AddInfrastructure();
            var sp = sc.BuildServiceProvider();
            var store = sp.GetRequiredService<IVolumeStore>();
            var seg = sp.GetRequiredService<ISegmentationEngine>();
            var nlp = sp.GetRequiredService<INlpReasoningService>();

            string nii = args.Length>0 ? args[0] : "datasets/samples/sample.nii";
            var vol = store.LoadAsync(new Imaging.ImageRef("MR", nii, null, null), default).Result;
            var res = seg.RunAsync(vol, new SegmentationOptions("models/segmentation/mock.onnx", 0.5f), default).Result;
            var ctx = new CaseContext("PSEUDO-001", "STUDY-001", null, res, null);
            var sum = nlp.SummarizeAsync(ctx, default).Result;
            Infrastructure.Reports.PdfReportService.ExportCaseReport("CaseReport.pdf", sum);
            Console.WriteLine("Done. CaseReport.pdf created.");
        }
    }
}
