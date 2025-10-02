using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MedicalAI.Infrastructure.DI;
using MedicalAI.Core;
using MedicalAI.Core.ML;
using System.Threading.Tasks;

namespace MedicalAI.Application.Tests
{
    public class SegmentationFlowTests
    {
        [Fact]
        public async Task MockSegmentationPipeline_Works()
        {
            var sp = new ServiceCollection().AddInfrastructure().BuildServiceProvider();
            var store = sp.GetRequiredService<MedicalAI.Core.Imaging.IVolumeStore>();
            var engine = sp.GetRequiredService<ISegmentationEngine>();
            var vol = await store.LoadAsync(new MedicalAI.Core.Imaging.ImageRef("MR","datasets/samples/sample.nii",null,null), default);
            var res = await engine.RunAsync(vol, new SegmentationOptions("models/segmentation/mock.onnx", 0.5f), default);
            res.Mask.Labels.Length.Should().Be(vol.Voxels.Length);
        }
    }
}
