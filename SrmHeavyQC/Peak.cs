namespace SrmHeavyQC
{
    public class Peak
    {
        public Peak(double mz, double intensity)
        {
            Mz = mz;
            Intensity = intensity;
        }
        public double Mz { get; }
        public double Intensity { get; }
    }
}
