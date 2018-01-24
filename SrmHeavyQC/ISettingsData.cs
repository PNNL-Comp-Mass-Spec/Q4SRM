namespace SrmHeavyQC
{
    public interface ISettingsData
    {
        double DefaultThreshold { get; set; }

        string CompoundThresholdFilePath { get; set; }

        string CompoundThresholdFileSha1Hash { get; set; }
    }
}
