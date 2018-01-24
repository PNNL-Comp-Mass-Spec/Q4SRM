using System.Collections.Generic;
using System.IO;
using PRISM;
using ReactiveUI;

namespace SrmHeavyChecker
{
    public class GuiOptions : ReactiveObject, IOptions
    {
        private string rawFilePath;
        private double defaultThreshold;
        private string compoundThresholdFilePath;
        private string outputFolder;
        private int maxThreads;
        private bool useOutputFolder;
        private bool useCompoundThresholdsFile;
        private bool overwriteOutput;

        public string RawFilePath
        {
            get { return rawFilePath; }
            set { this.RaiseAndSetIfChanged(ref rawFilePath, value); }
        }

        public double DefaultThreshold
        {
            get { return defaultThreshold; }
            set { this.RaiseAndSetIfChanged(ref defaultThreshold, value); }
        }

        public string CompoundThresholdFilePath
        {
            get { return compoundThresholdFilePath; }
            set { this.RaiseAndSetIfChanged(ref compoundThresholdFilePath, value); }
        }

        public string OutputFolder
        {
            get { return outputFolder; }
            set { this.RaiseAndSetIfChanged(ref outputFolder, value); }
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

        public List<string> FilesToProcessList { get; } = new List<string>();
        public int MaxThreadsUsable { get; }

        public string CompoundThresholdFileSha1Hash { get; set; }

        public IList<string> FilesToProcess => FilesToProcessList;

        public GuiOptions()
        {
            RawFilePath = "";
            CompoundThresholdFilePath = "";
            DefaultThreshold = 20;
            MaxThreads = SystemInfo.GetCoreCount();
            UseOutputFolder = false;
            UseCompoundThresholdsFile = false;
            MaxThreadsUsable = SystemInfo.GetLogicalCoreCount();
        }

        public string Validate()
        {
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

            return null;
        }
    }
}
