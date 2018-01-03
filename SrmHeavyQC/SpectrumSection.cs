using System.Collections.Generic;
using System.Linq;

namespace SrmHeavyQC
{
    public class SpectrumSection
    {
        public SpectrumSection(int scan)
        {
            Scan = scan;
        }

        public int Scan { get; }

        public List<Peak> SpectrumPeaks { get; } = new List<Peak>();

        public double GetPeakArea()
        {
            return SpectrumPeaks.Sum(x => x.Intensity);
        }
    }
}
