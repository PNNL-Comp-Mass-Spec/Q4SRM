using System;
using System.Collections.Generic;
using System.IO;
using PRISM;
using SrmHeavyQC;

namespace SrmHeavyChecker
{
    public class Options
    {
        [Option("raw", Required = true, HelpShowsDefault = false, HelpText = "Path to .raw file, or directory containing .raw files")]
        public string RawFilePath { get; set; }

        [Option("recurse", Required = false, HelpText = "If raw file path is a folder, whether to find .raw files in subfolders")]
        public bool Recurse { get; set; }

        [Option("filter", Required = false, HelpText = "If raw file path is a folder, a file filter string (supports '*' wildcard)")]
        public string FileFilter { get; set; }

        [Option("t", Required = false, HelpText = "Peak area threshold for a compound to be considered \"passing\"", Min = 0)]
        public double DefaultThreshold { get; set; }

        [Option("tpc", Required = false, HelpText = "A TSV file with compound and threshold columns, for custom thresholds for the specified compounds")]
        public string CompoundThresholdFilePath { get; set; }

        [Option("ppmTol", Required = false, HelpText = "PPM tolerance for matching peaks to compounds", Min = 0)]
        public double PpmTolerance { get; set; }

        [Option("out", Required = false, HelpText = "Folder where the result files should be written (default: written to same folder as .raw)", HelpShowsDefault = false)]
        public string OutputFolder { get; set; }

        [Option("threads", Required = false, HelpText = "Maximum number of threads to use (files processed simultaneously), '0' for automatic", Min = 0)]
        public int MaxThreads { get; set; }

        public List<string> FilesToProcess { get; }

        public List<CompoundThresholdData> CompoundThresholds { get; }
        public Dictionary<string, CompoundThresholdData> CompoundThresholdsLookup { get; private set; }

        public Options()
        {
            RawFilePath = "";
            CompoundThresholdFilePath = "";
            DefaultThreshold = 20;
            PpmTolerance = 20;
            FilesToProcess = new List<string>();
            CompoundThresholds = new List<CompoundThresholdData>();
            Recurse = false;
            FileFilter = "*.raw";
            MaxThreads = 0;
        }

        public bool Validate()
        {
            if (RawFilePath.ToLower().EndsWith(".raw"))
            {
                if (!File.Exists(RawFilePath))
                {
                    Console.WriteLine("ERROR: File \"{0}\" does not exist!", RawFilePath);
                    return false;
                }

                FilesToProcess.Add(RawFilePath);
            }
            else if (!Directory.Exists(RawFilePath))
            {
                Console.WriteLine("ERROR: Directory \"{0}\" does not exist!", RawFilePath);
                return false;
            }
            else if (Directory.Exists(RawFilePath))
            {
                var searchOption = Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var toProcess = Directory.EnumerateFiles(RawFilePath, FileFilter, searchOption);
                FilesToProcess.AddRange(toProcess);
            }
            else
            {
                Console.WriteLine("ERROR: Cannot process file \"{0}\"!", RawFilePath);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(CompoundThresholdFilePath))
            {
                if (!File.Exists(CompoundThresholdFilePath))
                {
                    Console.WriteLine("ERROR: Custom compound threshold file \"{0}\" does not exist!", RawFilePath);
                    return false;
                }

                var thresholds = CompoundThresholdData.ReadFromFile(CompoundThresholdFilePath);
                CompoundThresholds.AddRange(thresholds);
                CompoundThresholdsLookup = CompoundThresholdData.ConvertToSearchMap(CompoundThresholds);
            }

            if (!string.IsNullOrWhiteSpace(OutputFolder) && !Directory.Exists(OutputFolder))
            {
                try
                {
                    Directory.CreateDirectory(OutputFolder);
                }
                catch
                {
                    Console.WriteLine("ERROR: Output folder \"{0}\" does not exist, and could not be created!", OutputFolder);
                    return false;
                }
            }

            if (MaxThreads == 0)
            {
                MaxThreads = SystemInfo.GetCoreCount();
            }

            return true;
        }
    }
}
