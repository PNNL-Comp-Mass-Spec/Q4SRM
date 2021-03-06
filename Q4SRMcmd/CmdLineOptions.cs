﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PRISM;
using Q4SRM.DataReaders;
using Q4SRM.Output;
using Q4SRM.Settings;

namespace Q4SRMcmd
{
    public class CmdLineOptions : IOptions
    {
        private const string SummaryStatsFileDefaultName = "Q4SRMSummary.tsv";

        [Option("raw", Required = true, HelpShowsDefault = false, HelpText = "Path to .raw file, or directory containing .raw files")]
        public string RawFilePath { get; set; }

        [Option("recurse", Required = false, HelpText = "If raw file path is a folder, whether to find .raw files in subfolders")]
        public bool Recurse { get; set; }

        [Option("filter", Required = false, HelpText = "If raw file path is a folder, a file filter string (supports '*' wildcard)")]
        public string FileFilter { get; set; }

        [Option("method", Required = false, HelpText = "For mzML files, the method file path - TSV/CSV file, required column headers are 'Compound Name', 'Precursor (m/z)', 'Product (m/z)', 'Start Time (min)', 'End Time (min)'", HelpShowsDefault = false)]
        public string MethodFilePath { get; set; }

        [Option("t", Required = false, HelpText = "Peak area threshold for a compound to be considered \"passing\"", Min = 0)]
        public double DefaultIntensityThreshold { get; set; }

        [Option("m", Required = false, HelpText = "Time (in minutes) that a peak must be away from the edge of the target window for it to be considered \"passing\"", Min = 0)]
        public double EdgeNETThresholdMinutes { get; set; }

        [Option("ec", Required = false, HelpText = "Threshold (in minutes) for elution concurrence of the transition peaks for the same compound; smaller is stricter.", Min = 0)]
        public double ElutionConcurrenceThresholdMinutes { get; set; }

        [Option("sn", Required = false, HelpText = "Threshold for the Signal-to-Noise heuristic; larger is stricter; value is calculated as max intensity / median intensity", Min = 1)]
        public double SignalToNoiseHeuristicThreshold { get; set; }

        [Option("tpc", Required = false, HelpText = "A TSV file with compound and threshold columns, for custom thresholds for the specified compounds")]
        public string CompoundThresholdFilePath { get; set; }

        [Option("out", Required = false, HelpText = "Folder where the result files should be written (default: written to same folder as .raw)", HelpShowsDefault = false)]
        public string OutputFolder { get; set; }

        [Option("threads", Required = false, HelpText = "Maximum number of threads to use (files processed simultaneously), '0' for automatic", Min = 0)]
        public int MaxThreads { get; set; }

        [Option("ow", "overwrite", Required = false, HelpText = "If specified, all files will be processed, even if existing output was created with the same settings.")]
        public bool OverwriteOutput { get; set; }

        [Option("outThresholds", Required = false, HelpText = "If specified, creates a per-compound thresholds file for all compounds that pass the minimum threshold (averaged across processed files)")]
        public bool CreateThresholdsFile { get; set; }

        [Option("outThresholdsPct", Required = false, HelpText = "The percentage of the averaged total intensity that should be output as the threshold", Min = 0.01, Max = 0.99)]
        public double CreatedThresholdsFileThresholdLevel { get; set; }

        [Option("outThresholdsPath", Required = false, HelpText = "The path where the output threshold file should be created", HelpShowsDefault = false)]
        public string CompoundThresholdOutputFilePath { get; set; }

        public string CompoundThresholdFileSha1Hash { get; set; }

        [Option("summaryPath", Required = false, HelpText = "Path where processing summary tsv should be written. Default is [out folder/raw file folder]\\Q4SRMSummary.tsv", HelpShowsDefault = false)]
        public string SummaryStatsFilePath { get; set; }

        [Option("img", Required = false, HelpText = "Format for the saved total intensity vs. time plot")]
        public Plotting.ExportFormat ImageSaveFormat { get; set; }

        [Option("all", Required = false, HelpText = "If specified, metrics are computed for all compounds in the file (not just heavy compounds)")]
        public bool CheckAllCompounds { get; set; }

        public List<string> FilesToProcessList { get; }
        public IList<string> FilesToProcess => FilesToProcessList;

        public CmdLineOptions()
        {
            RawFilePath = "";
            MethodFilePath = "";
            CompoundThresholdFilePath = "";
            FilesToProcessList = new List<string>();
            Recurse = false;
            FileFilter = "";
            MaxThreads = 0;
            OverwriteOutput = false;
            ImageSaveFormat = Plotting.ExportFormat.PNG;
            this.SetDefaults();
        }

        public bool Validate()
        {
            var paths = ReaderLoader.GetDatasetPathsInPath(RawFilePath, Recurse);
            if (!string.IsNullOrWhiteSpace(FileFilter))
            {
                // match the filter; replace "." with "\.", "*" with ".*", and "?" with ".", and use compiled regex.
                var regexFilterString = FileFilter.Replace(".", @"\.").Replace("*", ".*").Replace("?", ".");
                var regexFilter = new Regex(regexFilterString, RegexOptions.Compiled | RegexOptions.IgnoreCase);

                FilesToProcessList.AddRange(paths.Where(x => regexFilter.IsMatch(Path.GetFileName(x))));
            }
            else
            {
                FilesToProcessList.AddRange(paths);
            }

            if (FilesToProcessList.Count == 0)
            {
                Console.WriteLine("ERROR: Cannot process file \"{0}\"! (No files to process)", RawFilePath);
                return false;
            }

            if (FilesToProcessList.Any(x =>
                    x.EndsWith(".mzml", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".mzml.gz", StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(MethodFilePath) || !File.Exists(MethodFilePath)))
            {
                Console.WriteLine("ERROR: Method file required for mzML files.");
                return false;
            }

            /*if (RawFilePath.ToLower().EndsWith(".raw"))
            {
                if (!File.Exists(RawFilePath))
                {
                    Console.WriteLine("ERROR: File \"{0}\" does not exist!", RawFilePath);
                    return false;
                }

                FilesToProcessList.Add(RawFilePath);
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
                FilesToProcessList.AddRange(toProcess);
            }
            else
            {
                Console.WriteLine("ERROR: Cannot process file \"{0}\"!", RawFilePath);
                return false;
            }*/

            if (!string.IsNullOrWhiteSpace(CompoundThresholdFilePath) && !File.Exists(CompoundThresholdFilePath))
            {
                Console.WriteLine("ERROR: Custom compound threshold file \"{0}\" does not exist!", CompoundThresholdFilePath);
                return false;
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

            if (CreateThresholdsFile)
            {
                if (string.IsNullOrWhiteSpace(CompoundThresholdOutputFilePath))
                {
                    Console.WriteLine("ERROR: When creating a thresholds output file, a path must be provided!");
                    return false;
                }

                var directory = Path.GetDirectoryName(CompoundThresholdOutputFilePath);
                if (!Directory.Exists(directory))
                {
                    Console.WriteLine("ERROR: Thresholds output file folder does not exist!: \"{0}\"", directory);
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(SummaryStatsFilePath))
            {
                if (!File.Exists(SummaryStatsFilePath))
                {
                    var directory = Path.GetDirectoryName(SummaryStatsFilePath);
                    if (string.IsNullOrWhiteSpace(directory))
                    {
                        directory = ".";
                    }

                    if (!Directory.Exists(directory))
                    {
                        Console.WriteLine("EEROR: Cannot write summary file to \"{0}\": Directory does not exist.", SummaryStatsFilePath);
                        return false;
                    }
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(OutputFolder))
                {
                    SummaryStatsFilePath = Path.Combine(OutputFolder, SummaryStatsFileDefaultName);
                }
                else if (Directory.Exists(RawFilePath))
                {
                    SummaryStatsFilePath = Path.Combine(RawFilePath, SummaryStatsFileDefaultName);
                }
                else
                {
                    var directory = Path.GetDirectoryName(RawFilePath);
                    if (string.IsNullOrWhiteSpace(directory))
                    {
                        SummaryStatsFilePath = SummaryStatsFileDefaultName;
                    }
                    else
                    {
                        SummaryStatsFilePath = Path.Combine(directory, SummaryStatsFileDefaultName);
                    }
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
