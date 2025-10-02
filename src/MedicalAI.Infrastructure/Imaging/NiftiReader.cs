using System;
using System.IO;
using System.Text;
using MedicalAI.Core;

namespace MedicalAI.Infrastructure.Imaging
{
    // Minimal NIfTI-1 single-file (.nii) reader for uint8 volumes
    public static class NiftiReader
    {
        // NIfTI-1 Header Constants
        private const int HeaderSize = 348;
        private const int DimInfoOffset = 40;
        private const int DatatypeOffset = 70;
        private const int PixDimOffset = 76;
        private const int VoxOffsetOffset = 108;
        private const int MagicOffset = 344;

        // NIfTI-1 Datatype Constants
        private const short DtUint8 = 2;
        private const short BitPixUint8 = 8;

        // NIfTI-1 Magic Strings
        private const string MagicN1 = "n+1";
        private const string MagicNi1 = "ni1";

        public static Volume3D Read(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);

            ValidateHeaderSize(br);

            fs.Seek(DimInfoOffset, SeekOrigin.Begin);
            br.ReadInt16(); // dim[0]
            var width = br.ReadInt16();
            var height = br.ReadInt16();
            var depth = br.ReadInt16();

            ValidateDataType(br);

            fs.Seek(PixDimOffset, SeekOrigin.Begin);
            br.ReadSingle(); // pixdim[0] is unused
            var vx = br.ReadSingle();
            var vy = br.ReadSingle();
            var vz = br.ReadSingle();

            fs.Seek(VoxOffsetOffset, SeekOrigin.Begin);
            var voxOffset = br.ReadSingle();

            ValidateMagicNumber(br);

            fs.Seek(Convert.ToInt32(voxOffset), SeekOrigin.Begin);
            var totalVoxels = width * height * depth;
            var data = br.ReadBytes(totalVoxels);

            return new Volume3D(width, height, depth, vx, vy, vz, data);
        }

        private static void ValidateHeaderSize(BinaryReader br)
        {
            var sizeofHdr = br.ReadInt32();
            if (sizeofHdr != HeaderSize)
            {
                throw new InvalidDataException($"Bad NIfTI header size. Expected {HeaderSize}, but got {sizeofHdr}.");
            }
        }

        private static void ValidateDataType(BinaryReader br)
        {
            br.BaseStream.Seek(DatatypeOffset, SeekOrigin.Begin);
            var datatype = br.ReadInt16();
            var bitpix = br.ReadInt16();
            if (datatype != DtUint8 || bitpix != BitPixUint8)
            {
                throw new NotSupportedException("Only uint8 data type is supported in this reader.");
            }
        }

        private static void ValidateMagicNumber(BinaryReader br)
        {
            br.BaseStream.Seek(MagicOffset, SeekOrigin.Begin);
            var magic = Encoding.ASCII.GetString(br.ReadBytes(4));
            if (!magic.StartsWith(MagicN1) && !magic.StartsWith(MagicNi1))
            {
                throw new InvalidDataException($"Invalid NIfTI magic number. Expected '{MagicN1}' or '{MagicNi1}', but got '{magic}'.");
            }
        }
    }
}