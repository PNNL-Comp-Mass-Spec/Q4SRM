using SrmHeavyQC;

namespace SrmHeavyQCTests
{
    internal class SettingsData : ISettingsData
    {
        public double DefaultThreshold { get; set; }

        public double EdgeNETThresholdMinutes { get; set; }

        public string CompoundThresholdFilePath { get; set; }

        public string CompoundThresholdFileSha1Hash { get; set; }
    }
}
