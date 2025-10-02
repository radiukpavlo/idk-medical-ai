using Xunit;
using FluentAssertions;
using MedicalAI.Core.Math;

namespace MedicalAI.Core.Tests
{
    public class MetricsTests
    {
        [Fact]
        public void DiceAndIoU_WorkOnSimpleArrays()
        {
            var a = new byte[]{1,0,1,0};
            var b = new byte[]{1,1,0,0};
            SegmentationMetrics.Dice(a,b).Should().BeApproximately(0.5, 1e-6);
            SegmentationMetrics.IoU(a,b).Should().BeApproximately(1.0/3.0, 1e-6);
        }
    }
}
