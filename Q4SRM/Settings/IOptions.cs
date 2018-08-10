using System.Collections.Generic;
using Q4SRM.Output;

namespace Q4SRM.Settings
{
    public interface IOptions : ISettingsData
    {
        string RawFilePath { get; }

        string OutputFolder { get; }

        int MaxThreads { get; }

        IList<string> FilesToProcess { get; }

        bool OverwriteOutput { get; }

        bool CreateThresholdsFile { get; }

        double CreatedThresholdsFileThresholdLevel { get; }

        string CompoundThresholdOutputFilePath { get; }

        string SummaryStatsFilePath { get; }

        Plotting.ExportFormat ImageSaveFormat { get; }
    }
}
