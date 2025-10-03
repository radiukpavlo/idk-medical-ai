using Microsoft.Extensions.Logging.Abstractions;
using MedicalAI.Core.Performance;
using MedicalAI.Infrastructure.Performance;
using MedicalAI.Infrastructure.Imaging;
using MedicalAI.Infrastructure.ML;
using Classification.KIGCN;
using Segmentation.SKIFSeg;

namespace MedicalAI.UI.Tests
{
    internal static class TestHelpers
    {
        public static IMemoryManager CreateMemoryManager() => new MemoryManager(NullLogger<MemoryManager>.Instance);
        public static IParallelProcessor CreateParallelProcessor() => new ParallelProcessor(NullLogger<ParallelProcessor>.Instance);

        public static VolumeStore CreateVolumeStore(IMemoryManager mem)
        {
            var lim = new LargeImageManager(NullLogger<LargeImageManager>.Instance, mem);
            return new VolumeStore(NullLogger<VolumeStore>.Instance, lim, mem);
        }

        public static MockSegmentationEngine CreateSegEngine(IMemoryManager mem)
            => new MockSegmentationEngine(NullLogger<MockSegmentationEngine>.Instance, CreateParallelProcessor(), mem);

        public static DicomImportService CreateDicomImportService(IMemoryManager mem)
            => new DicomImportService(NullLogger<DicomImportService>.Instance, CreateParallelProcessor(), mem);

        public static DicomAnonymizerService CreateDicomAnonymizerService(IMemoryManager mem)
            => new DicomAnonymizerService(NullLogger<DicomAnonymizerService>.Instance, CreateParallelProcessor(), mem);

        public static KigcnEngine CreateKigcn()
            => new KigcnEngine(NullLogger<MedicalAI.Infrastructure.ML.MockClassificationEngine>.Instance, CreateParallelProcessor());

        public static SkifSegEngine CreateSkifSeg(IMemoryManager mem)
            => new SkifSegEngine(NullLogger<MedicalAI.Infrastructure.ML.MockSegmentationEngine>.Instance, CreateParallelProcessor(), mem);
    }
}

