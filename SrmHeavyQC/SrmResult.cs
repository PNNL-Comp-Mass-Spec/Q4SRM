namespace SrmHeavyQC
{
    public class SrmResult
    {
        public double TolerancePPM { get; }

        public SrmTableData Transition { get; }

        public double TargetMz { get; }
        public double MinMz { get; }
        public double MaxMz { get; }

        public double Area { get; set; }

        public SrmResult(SrmTableData transition, double tolerancePpm = 20)
        {
            Transition = transition;
            TolerancePPM = tolerancePpm;

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
    }
}
