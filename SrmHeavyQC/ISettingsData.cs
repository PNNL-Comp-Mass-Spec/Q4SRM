﻿namespace SrmHeavyQC
{
    public interface ISettingsData
    {
        double DefaultThreshold { get; set; }

        double EdgeNETThresholdMinutes { get; set; }

        /// <summary>
        /// Path to the per-compound threshold file. It is the responsibility of the implementation to verify the file exists, if this is not blank.
        /// </summary>
        string CompoundThresholdFilePath { get; set; }

        string CompoundThresholdFileSha1Hash { get; set; }
    }
}
