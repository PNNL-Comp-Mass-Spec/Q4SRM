using System;
using System.Collections.Generic;
using System.Linq;

namespace SrmHeavyQC
{
    public class SrmResult
    {
        public const double TolerancePPM = 20;

        public SrmTableData Transition { get; }

        [Obsolete()]
        public List<SpectrumSection> SpectraResults { get; } = new List<SpectrumSection>();

        public double TargetMz { get; }
        public double MinMz { get; }
        public double MaxMz { get; }

        public double Area { get; set; }

        public SrmResult(SrmTableData transition)
        {
            Transition = transition;

            //TargetMz = Transition.PrecursorMz; // No results
            TargetMz = Transition.ProductMz;
            var tolerance = TargetMz * TolerancePPM / 1e6;
            MinMz = TargetMz - tolerance;
            MaxMz = TargetMz + tolerance;
            Area = 0;
        }

        /// <summary>
        /// "Blank" SrmResult
        /// </summary>
        public SrmResult()
        {
            TargetMz = 0;
            MinMz = 0;
            MaxMz = 0;
            Transition = new SrmTableData();
        }

        [Obsolete()]
        public double GetSummedArea()
        {
            return SpectraResults.Sum(x => x.GetPeakArea());
        }
    }
}
