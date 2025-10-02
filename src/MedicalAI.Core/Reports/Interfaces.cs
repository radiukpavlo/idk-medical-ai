using MedicalAI.Core.ML;

namespace MedicalAI.Core.Reports
{
    public interface IReportService
    {
        void ExportCaseReport(string path, NlpSummary summary);
    }
}