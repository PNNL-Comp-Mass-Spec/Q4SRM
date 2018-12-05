namespace Q4SRM.Settings
{
    public interface ISettingsData
    {
        double DefaultIntensityThreshold { get; set; }

        double EdgeNETThresholdMinutes { get; set; }

        double ElutionConcurrenceThresholdMinutes { get; set; }
        double SignalToNoiseHeuristicThreshold { get; set; }

        /// <summary>
        /// Path to the per-compound threshold file. It is the responsibility of the implementation to verify the file exists, if this is not blank.
        /// </summary>
        string CompoundThresholdFilePath { get; set; }

        string CompoundThresholdFileSha1Hash { get; set; }

        bool CheckAllCompounds { get; set; }
    }
}
