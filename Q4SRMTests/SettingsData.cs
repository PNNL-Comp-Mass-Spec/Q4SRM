﻿using Q4SRM.Settings;

namespace Q4SRMTests
{
    internal class SettingsData : ISettingsData
    {
        public double DefaultIntensityThreshold { get; set; }

        public double EdgeNETThresholdMinutes { get; set; }

        public double ElutionConcurrenceThresholdMinutes { get; set; }

        public double SignalToNoiseHeuristicThreshold { get; set; }

        public string CompoundThresholdFilePath { get; set; }

        public string CompoundThresholdFileSha1Hash { get; set; }

        public bool CheckAllCompounds { get; set; }
    }
}
