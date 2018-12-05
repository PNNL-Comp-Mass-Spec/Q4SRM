using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using PRISM;
using Q4SRM.Output;
using Q4SRM.Settings;
using ReactiveUI;

namespace Q4SRMui
{
    public class GuiOptions : ReactiveObject, IOptions
    {
        public const string SummaryStatsFileDefaultName = "Q4SRMSummary.tsv";
        private string methodFilePath;
        private double defaultThreshold;
        private double edgeNETThresholdMinutes;
        private double elutionConcurrenceThresholdMinutes;
        private double signalToNoiseHeuristicThreshold;
        private string compoundThresholdFilePath;
        private string outputFolder;
        private int maxThreads;
        private bool useOutputFolder;
        private bool useCompoundThresholdsFile;
        private bool overwriteOutput;
        private bool createThresholdsFile;
        private double createdThresholdsFileThresholdLevel;
        private string compoundThresholdOutputFilePath;
        private string summaryStatsFilePath;
        private Plotting.ExportFormat imageSaveFormat;
        private bool checkAllCompounds;

        public string MethodFilePath
        {
            get { return methodFilePath; }
            set { this.RaiseAndSetIfChanged(ref methodFilePath, value); }
        }

        public double DefaultIntensityThreshold
        {
            get { return defaultThreshold; }
            set { this.RaiseAndSetIfChanged(ref defaultThreshold, value); }
        }

        public double EdgeNETThresholdMinutes
        {
            get { return edgeNETThresholdMinutes; }
            set { this.RaiseAndSetIfChanged(ref edgeNETThresholdMinutes, value); }
        }

        public double ElutionConcurrenceThresholdMinutes
        {
            get => elutionConcurrenceThresholdMinutes;
            set { this.RaiseAndSetIfChanged(ref elutionConcurrenceThresholdMinutes, value); }
        }

        public double SignalToNoiseHeuristicThreshold
        {
            get => signalToNoiseHeuristicThreshold;
            set { this.RaiseAndSetIfChanged(ref signalToNoiseHeuristicThreshold, value); }
        }

        public string CompoundThresholdFilePath
        {
            get { return compoundThresholdFilePath; }
            set { this.RaiseAndSetIfChanged(ref compoundThresholdFilePath, value); }
        }

        public string OutputFolder
        {
            get { return outputFolder; }
            set
            {
                var oldValue = outputFolder;
                this.RaiseAndSetIfChanged(ref outputFolder, value);
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    UpdateSummaryStatsFilePath(oldValue, value);
                });
            }
        }

        public int MaxThreads
        {
            get { return maxThreads; }
            set { this.RaiseAndSetIfChanged(ref maxThreads, value); }
        }

        public bool UseOutputFolder
        {
            get { return useOutputFolder; }
            set { this.RaiseAndSetIfChanged(ref useOutputFolder, value); }
        }

        public bool UseCompoundThresholdsFile
        {
            get { return useCompoundThresholdsFile; }
            set { this.RaiseAndSetIfChanged(ref useCompoundThresholdsFile, value); }
        }

        public bool OverwriteOutput
        {
            get { return overwriteOutput; }
            set { this.RaiseAndSetIfChanged(ref overwriteOutput, value); }
        }

        public bool CreateThresholdsFile
        {
            get { return createThresholdsFile; }
            set { this.RaiseAndSetIfChanged(ref createThresholdsFile, value); }
        }

        public double CreatedThresholdsFileThresholdLevel
        {
            get { return createdThresholdsFileThresholdLevel; }
            set { this.RaiseAndSetIfChanged(ref createdThresholdsFileThresholdLevel, value); }
        }

        public string CompoundThresholdOutputFilePath
        {
            get { return compoundThresholdOutputFilePath; }
            set { this.RaiseAndSetIfChanged(ref compoundThresholdOutputFilePath, value); }
        }

        public string SummaryStatsFilePath
        {
            get { return summaryStatsFilePath; }
            set { this.RaiseAndSetIfChanged(ref summaryStatsFilePath, value); }
        }

        public Plotting.ExportFormat ImageSaveFormat
        {
            get { return imageSaveFormat; }
            set { this.RaiseAndSetIfChanged(ref imageSaveFormat, value); }
        }

        public bool CheckAllCompounds
        {
            get { return checkAllCompounds; }
            set { this.RaiseAndSetIfChanged(ref checkAllCompounds, value); }
        }

        public List<string> FilesToProcessList { get; } = new List<string>();
        public int MaxThreadsUsable { get; }

        public string CompoundThresholdFileSha1Hash { get; set; }

        public IList<string> FilesToProcess => FilesToProcessList;

        public GuiOptions()
        {
            CompoundThresholdFilePath = "";
            MaxThreads = SystemInfo.GetCoreCount();
            UseOutputFolder = false;
            UseCompoundThresholdsFile = false;
            MaxThreadsUsable = SystemInfo.GetLogicalCoreCount();
            CreateThresholdsFile = false;
            SummaryStatsFilePath = SummaryStatsFileDefaultName;
            ImageSaveFormat = Plotting.ExportFormat.PNG;

            this.SetDefaults();
        }

        /// <summary>
        /// Updates the output folder path if the user hasn't changed it directly
        /// </summary>
        /// <param name="oldPath"></param>
        /// <param name="newPath"></param>
        public void UpdateOutputFolder(string oldPath, string newPath)
        {
            if (string.IsNullOrWhiteSpace(OutputFolder))
            {
                OutputFolder = newPath;
            }

            if (oldPath != null && oldPath.Equals(OutputFolder, StringComparison.OrdinalIgnoreCase))
            {
                OutputFolder = newPath;
            }
        }

        /// <summary>
        /// Updates the summary stats file path, replacing <paramref name="oldOutputPath"/> with <paramref name="newOutputPath"/>
        /// </summary>
        /// <param name="oldOutputPath"></param>
        /// <param name="newOutputPath"></param>
        public void UpdateSummaryStatsFilePath(string oldOutputPath, string newOutputPath)
        {
            if (SummaryStatsFilePath.Equals(SummaryStatsFileDefaultName))
            {
                SummaryStatsFilePath = Path.Combine(newOutputPath, SummaryStatsFileDefaultName);
            }
            if (oldOutputPath != null && oldOutputPath.Equals(newOutputPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var currentDir = Path.GetDirectoryName(SummaryStatsFilePath);
            var currentName = Path.GetFileName(SummaryStatsFilePath);
            if (string.IsNullOrWhiteSpace(currentDir))
            {
                SummaryStatsFilePath = Path.Combine(newOutputPath, SummaryStatsFilePath);
                return;
            }

            if (currentDir.Equals(oldOutputPath, StringComparison.OrdinalIgnoreCase))
            {
                SummaryStatsFilePath = Path.Combine(newOutputPath, currentName);
            }
        }

        public string Validate()
        {
            if (FilesToProcessList.Any(x =>
                    x.EndsWith("mzml", StringComparison.OrdinalIgnoreCase) || x.EndsWith("mzml.gz", StringComparison.OrdinalIgnoreCase)) &&
                string.IsNullOrWhiteSpace(MethodFilePath) || !File.Exists(MethodFilePath))
            {
                return "ERROR: Method file required for mzML files.";
            }

            if (UseCompoundThresholdsFile && !string.IsNullOrWhiteSpace(CompoundThresholdFilePath) && !File.Exists(CompoundThresholdFilePath))
            {
                return $"ERROR: Custom compound threshold file \"{CompoundThresholdFilePath}\" does not exist!";
            }

            if (UseOutputFolder && !string.IsNullOrWhiteSpace(OutputFolder) && !Directory.Exists(OutputFolder))
            {
                try
                {
                    Directory.CreateDirectory(OutputFolder);
                }
                catch
                {
                    return $"ERROR: Output folder \"{OutputFolder}\" does not exist, and could not be created!";
                }
            }

            if (CreateThresholdsFile)
            {
                if (string.IsNullOrWhiteSpace(CompoundThresholdOutputFilePath))
                {
                    return $"ERROR: When creating a thresholds output file, a path must be provided!";
                }

                var directory = Path.GetDirectoryName(CompoundThresholdOutputFilePath);
                if (!Directory.Exists(directory))
                {
                    return $"ERROR: Thresholds output file folder does not exist!: \"{directory}\"";
                }
            }


            return null;
        }
    }
}
