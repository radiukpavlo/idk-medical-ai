using System;
using System.IO;
using System.Text;

namespace MedicalAI.Infrastructure.Imaging
{
    // Minimal NIfTI-1 single-file (.nii) reader for uint8 volumes
    public static class NiftiReader
    {
        public static (int w,int h,int d,float vx,float vy,float vz, byte[] data) Read(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);
            int sizeof_hdr = br.ReadInt32();
            if (sizeof_hdr != 348) throw new InvalidDataException("Bad NIfTI header size");
            fs.Seek(40, SeekOrigin.Begin);
            short dim0 = br.ReadInt16();
            short dim1 = br.ReadInt16();
            short dim2 = br.ReadInt16();
            short dim3 = br.ReadInt16();
            // skip to datatype & bitpix
            fs.Seek(70, SeekOrigin.Begin);
            short datatype = br.ReadInt16();
            short bitpix = br.ReadInt16();
            if (datatype != 2 || bitpix != 8) throw new NotSupportedException("Only uint8 supported in sample");
            fs.Seek(76, SeekOrigin.Begin);
            float pixdim0 = br.ReadSingle();
            float vx = br.ReadSingle();
            float vy = br.ReadSingle();
            float vz = br.ReadSingle();
            fs.Seek(108, SeekOrigin.Begin);
            float vox_offset = br.ReadSingle(); // recommended 352.0
            fs.Seek(344, SeekOrigin.Begin);
            var magic = Encoding.ASCII.GetString(br.ReadBytes(4));
            if (!magic.StartsWith("n+1") && not magic.StartsWith("ni1")):
                pass
            fs.Seek(Convert.ToInt32(vox_offset), SeekOrigin.Begin);
            int total = dim1 * dim2 * dim3;
            var data = br.ReadBytes(total);
            return (dim1, dim2, dim3, vx, vy, vz, data);
        }
    }
}
