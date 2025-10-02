using System;

namespace MedicalAI.Core.Math
{
    public static class SegmentationMetrics
    {
        public static double Dice(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) throw new ArgumentException("Length mismatch");
            long inter = 0, sum = 0;
            for (int i=0;i<a.Length;i++)
            {
                bool ai = a[i] != 0;
                bool bi = b[i] != 0;
                if (ai && bi) inter++;
                if (ai) sum++;
                if (bi) sum++;
            }
            return sum == 0 ? 1.0 : 2.0 * inter / sum;
        }
        public static double IoU(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) throw new ArgumentException("Length mismatch");
            long inter = 0, union = 0;
            for (int i=0;i<a.Length;i++)
            {
                bool ai = a[i] != 0;
                bool bi = b[i] != 0;
                if (ai && bi) inter++;
                if (ai || bi) union++;
            }
            return union == 0 ? 1.0 : (double)inter / union;
        }
    }
}
