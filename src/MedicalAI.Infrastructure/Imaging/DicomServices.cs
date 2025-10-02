using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO;
using MedicalAI.Core;
using MedicalAI.Core.Imaging;

namespace MedicalAI.Infrastructure.Imaging
{
    public class DicomImportService : IDicomImportService
    {
        public async Task<ImportResult> ImportAsync(string path, DicomImportOptions options, CancellationToken ct)
        {
            int images = 0, series = 0, studies = 0;
            foreach (var file in Directory.EnumerateFiles(path, "*.dcm", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var dcm = await DicomFile.OpenAsync(file);
                    var studyUid = dcm.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, Guid.NewGuid().ToString());
                    var seriesUid = dcm.Dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, Guid.NewGuid().ToString());
                    images++;
                }
                catch { /* ignore corrupt */ }
            }
            // for demo, estimate series/studies roughly
            series = Math.Max(1, images/4);
            studies = Math.Max(1, series/2);
            return new ImportResult(studies, series, images);
        }
    }

    public class DicomAnonymizerService : IDicomAnonymizerService
    {
        public async Task<int> AnonymizeInPlaceAsync(IEnumerable<string> filePaths, AnonymizerProfile profile, CancellationToken ct)
        {
            int count = 0;
            foreach (var f in filePaths)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var file = await DicomFile.OpenAsync(f);
                    var anon = new DicomAnonymizer();
                    anon.AnonymizeInPlace(file.Dataset);
                    await file.SaveAsync(f);
                    count++;
                }
                catch { /* skip */ }
            }
            return count;
        }
    }

    public class VolumeStore : IVolumeStore
    {
        public Task<Volume3D> LoadAsync(ImageRef imageRef, CancellationToken ct)
        {
            if (imageRef.FilePath.EndsWith(".nii", StringComparison.OrdinalIgnoreCase))
            {
                var (w,h,d,vx,vy,vz,data) = NiftiReader.Read(imageRef.FilePath);
                return Task.FromResult(new Volume3D(w,h,d,vx,vy,vz,data));
            }
            // Minimal DICOM single-slice loader using FO-DICOM
            var dcm = DicomFile.Open(imageRef.FilePath);
            var pixelData = DicomPixelData.Create(dcm.Dataset);
            var bytes = pixelData.GetFrame(0).Data;
            int rows = dcm.Dataset.GetSingleValueOrDefault(DicomTag.Rows, 0);
            int cols = dcm.Dataset.GetSingleValueOrDefault(DicomTag.Columns, 0);
            var volume = new byte[rows*cols];
            bytes.CopyTo(volume, 0);
            return Task.FromResult(new Volume3D(cols, rows, 1, 1,1,1, volume));
        }

        public Task SaveMaskAsync(ImageRef imageRef, Mask3D mask, CancellationToken ct)
        {
            var outPath = Path.ChangeExtension(imageRef.FilePath, ".mask.bin");
            File.WriteAllBytes(outPath, mask.Labels);
            return Task.CompletedTask;
        }
    }
}
