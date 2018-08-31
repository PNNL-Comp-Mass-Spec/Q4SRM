using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Q4SRM.Data;
using Q4SRM.Output;
using Q4SRM.RawFileIO;
using Q4SRM.Settings;

namespace Q4SRM
{
    public class FileProcessor
    {
        public void RunProcessing(IOptions options, CancellationTokenSource cancelInformation, Action<string> statusUpdate = null)
        {
            if (options.MaxThreads == 1 || options.FilesToProcess.Count == 1)
            {
                foreach (var file in options.FilesToProcess)
                {
                    if (cancelInformation.IsCancellationRequested)
                    {
                        break;
                    }

                    ProcessFile(options, file, statusUpdate);
                }
            }
            else
            {
                var parallelOptions = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = options.MaxThreads,
                    CancellationToken = cancelInformation.Token,
                };
                Parallel.ForEach(options.FilesToProcess, parallelOptions, x => ProcessFile(options, x, statusUpdate));
            }

            CreateSummaryFile(options);
        }

        private string GetOutputFileForDataset(IOptions options, string rawFilePath)
        {
            var outputFileName = Path.GetFileNameWithoutExtension(rawFilePath) + "_heavyPeaks.tsv";
            var outputFolder = Path.GetDirectoryName(rawFilePath);
            if (!string.IsNullOrWhiteSpace(options.OutputFolder))
            {
                outputFolder = options.OutputFolder;
            }
            else if (string.IsNullOrWhiteSpace(outputFolder))
            {
                outputFolder = ".";
            }

            return Path.Combine(outputFolder, outputFileName);
        }

        private void ProcessFile(IOptions options, string rawFilePath, Action<string> statusUpdate = null)
        {
            var outputFilePath = GetOutputFileForDataset(options, rawFilePath);

            if (!options.OverwriteOutput && File.Exists(outputFilePath) && CompoundData.CheckSettings(outputFilePath, options))
            {
                Console.WriteLine("Skipping file \"{0}\"; existing output was created with matching settings", rawFilePath);
                return;
            }

            Console.WriteLine("Processing file \"{0}\"", rawFilePath);
            statusUpdate?.Invoke($"Processing file \"{rawFilePath}\"");
            using (var rawReader = new XCalDataReader(rawFilePath))
            {
                var results = rawReader.ReadRawData(options);
                if (results == null)
                {
                    return;
                }
                //Console.WriteLine("File \"{0}\": RawResults: {1}", rawFilePath, results.Count);
                //var combined = rawReader.AggregateResults(results, options.DefaultThreshold, CompoundThresholdsLookup);
                //Console.WriteLine("File \"{0}\": CombinedResults: {1}", rawFilePath, combined.Count);

                CompoundData.WriteCombinedResultsToFile(outputFilePath, results, options);
                Plotting.PlotResults(results, Path.GetFileNameWithoutExtension(rawFilePath), Path.ChangeExtension(outputFilePath, null) + "_summary", options.ImageSaveFormat);
                var pdfPath = Path.ChangeExtension(outputFilePath, "pdf");
                var pdfWriter = new PdfWriter(Path.GetFileNameWithoutExtension(rawFilePath), rawFilePath, pdfPath);
                pdfWriter.WritePdf(results, options);

                /*/
                var imagesDir = Path.ChangeExtension(outputFilePath, null) + "_images";
                if (!Directory.Exists(imagesDir))
                {
                    try
                    {
                        Directory.CreateDirectory(imagesDir);
                    }
                    catch { }
                }

                if (Directory.Exists(imagesDir))
                {
                    foreach (var compound in results)
                    {
                        // replace invalid characters with underscores
                        var name = Path.GetInvalidFileNameChars().Aggregate(compound.CompoundName, (current, c) => current.Replace(c.ToString(), "_"));
                        var namePrefix = (compound.PassesAllThresholds
                                             ? "P"
                                             : GetCompoundDataPrefix(compound)) + "_";
                        var pathBase = Path.Combine(imagesDir, namePrefix + name);
                        Plotting.PlotCompound(compound, pathBase + ".png", Plotting.ExportFormat.PDF);

                        foreach (var transition in compound.Transitions)
                        {
                            var path = $"{pathBase}_{transition.ProductMz:F2}.png";
                            Plotting.PlotTransition(transition, path, Plotting.ExportFormat.PDF);
                        }
                    }
                }
                /**/
            }
            Console.WriteLine("Finished Processing file \"{0}\"", rawFilePath);
            statusUpdate?.Invoke($"Finished Processing file \"{rawFilePath}\"");
        }

        private string GetCompoundDataPrefix(CompoundData result)
        {
            var items = new List<string>();
            if (!result.PassesIntensity)
            {
                items.Add("int");
            }
            if (!result.PassesNET)
            {
                items.Add("pp");
            }
            if (!result.PassesElutionConcurrence)
            {
                items.Add("ec");
            }
            if (!result.PassesSignalToNoiseHeuristic)
            {
                items.Add("sn");
            }

            return string.Join("_", items);
        }

        private void CreateSummaryFile(IOptions options)
        {
            var summaryData = new List<SummaryStats>();
            var allResults = new List<CompoundData>();
            foreach (var dataset in options.FilesToProcess)
            {
                var resultFilePath = GetOutputFileForDataset(options, dataset);
                if (!File.Exists(resultFilePath))
                {
                    continue;
                }
                var results = CompoundData.ReadCombinedResultsFile(resultFilePath).ToList();
                var datasetName = Path.GetFileName(dataset);

                summaryData.Add(new SummaryStats(datasetName, results));

                if (options.CreateThresholdsFile)
                {
                    allResults.AddRange(results);
                }
            }

            SummaryStats.WriteToFile(options.SummaryStatsFilePath, summaryData);

            if (options.CreateThresholdsFile)
            {
                CreateThresholdsFile(options, allResults);
            }
        }

        private void CreateThresholdsFile(IOptions options, List<CompoundData> fullResults)
        {
            Console.WriteLine("Creating per-compound thresholds file...");

            // Group the passing results
            var grouped = fullResults.Where(x => x.PassesIntensity).GroupBy(x => x.CompoundName);
            var thresholds = new List<CompoundThresholdData>();
            foreach (var group in grouped)
            {
                var groupData = group.ToList();
                var threshold = new CompoundThresholdData();
                threshold.CompoundName = group.Key;
                threshold.PrecursorMz = groupData[0].PrecursorMz;
                var averageTotalIntensity = groupData.Average(x => x.TotalIntensitySum);
                threshold.Threshold = averageTotalIntensity * options.CreatedThresholdsFileThresholdLevel;
                thresholds.Add(threshold);
            }

            CompoundThresholdData.WriteToFile(options.CompoundThresholdOutputFilePath, thresholds);
            Console.WriteLine("Created per-compound thresholds file.");
        }
    }
}
