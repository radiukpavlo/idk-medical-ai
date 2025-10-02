using System;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MedicalAI.Core.ML;

namespace MedicalAI.Infrastructure.Reports
{
    public static class PdfReportService
    {
        public static void ExportCaseReport(string path, NlpSummary summary)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(36);
                    page.Header().Row(r => {
                        r.RelativeItem().Text("MedicalAI Thesis Suite").Bold().FontSize(18);
                        r.ConstantItem(200).Text("ДЛЯ ДОСЛІДНИЦЬКОГО ВИКОРИСТАННЯ").SemiBold().FontColor(Colors.Grey.Medium);
                    });
                    page.Content().Text(summary.Text).FontSize(12);
                    page.Footer().AlignCenter().Text(x=>{
                        x.Span("Research use only. Not a medical device.");
                    });
                });
            }).GeneratePdf(path);
        }
    }
}
