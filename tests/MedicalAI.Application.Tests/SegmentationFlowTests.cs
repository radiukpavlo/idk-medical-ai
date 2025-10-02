using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MedicalAI.Infrastructure.DI;
using MedicalAI.Core;
using MedicalAI.Core.ML;

namespace MedicalAI.Application.Tests
{
    public class SegmentationFlowTests
    {
        [Fact]
        public void MockSegmentationPipeline_Works()
        {
            var sp = new ServiceCollection().AddInfrastructure().BuildServiceProvider();
            var store = sp.GetRequiredService<MedicalAI.Core.Imaging.IVolumeStore>();
            var engine = sp.GetRequiredService<ISegmentationEngine>();
            var vol = store.LoadAsync(new MedicalAI.Core.Imaging.ImageRef("MR","datasets/samples/sample.nii",null,null), default).Result;
            var res = engine.RunAsync(vol, new SegmentationOptions("models/segmentation/mock.onnx", 0.5f), default).Result;
            res.Mask.Labels.Length.Should().Be(vol.Voxels.Length);
        }
    }
}
