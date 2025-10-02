using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MedicalAI.Core.Imaging;

namespace MedicalAI.Application.Commands
{
    public record ImportDicomCommand(string Path, bool Anonymize) : IRequest<ImportResult>;

    public class ImportDicomHandler : IRequestHandler<ImportDicomCommand, ImportResult>
    {
        private readonly IDicomImportService _importer;
        public ImportDicomHandler(IDicomImportService importer) { _importer = importer; }
        public Task<ImportResult> Handle(ImportDicomCommand request, CancellationToken cancellationToken)
            => _importer.ImportAsync(request.Path, new DicomImportOptions(request.Anonymize), cancellationToken);
    }
}
