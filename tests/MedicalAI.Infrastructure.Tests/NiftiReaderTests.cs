using Xunit;
using FluentAssertions;
using MedicalAI.Infrastructure.Imaging;
using System.IO;

namespace MedicalAI.Infrastructure.Tests
{
    public class NiftiReaderTests
    {
        [Fact]
        public void CanReadSampleNifti()
        {
            var (w,h,d,vx,vy,vz,data) = NiftiReader.Read("datasets/samples/sample.nii");
            w.Should().Be(16); h.Should().Be(16); d.Should().Be(8);
            data.Length.Should().Be(w*h*d);
        }
    }
}
