using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SrmHeavyQC;

namespace SrmHeavyChecker
{
    public class FileProcessor
    {
        public List<CompoundThresholdData> CompoundThresholds { get; }
        public Dictionary<string, CompoundThresholdData> CompoundThresholdsLookup { get; private set; }

        public FileProcessor()
        {
            CompoundThresholds = new List<CompoundThresholdData>();
        }

        public void RunProcessing(IOptions options, CancellationTokenSource cancelInformation)
        {
            if (options.MaxThreads == 1 || options.FilesToProcess.Count == 1)
            {
                foreach (var file in options.FilesToProcess)
                {
                    if (cancelInformation.IsCancellationRequested)
                    {
                        break;
                    }

                    ProcessFile(options, file);
                }
            }
            else
            {
                var parallelOptions = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = options.MaxThreads,
                    CancellationToken = cancelInformation.Token,
                };
                Parallel.ForEach(options.FilesToProcess, parallelOptions, x => ProcessFile(options, x));
            }
        }

        public void LoadCompoundThresholds(IOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.CompoundThresholdFilePath))
            {
                if (!File.Exists(options.CompoundThresholdFilePath))
                {
                    Console.WriteLine("ERROR: Custom compound threshold file \"{0}\" does not exist!", options.CompoundThresholdFilePath);
                    return;
                }

                var thresholds = CompoundThresholdData.ReadFromFile(options.CompoundThresholdFilePath);
                CompoundThresholds.AddRange(thresholds);
                CompoundThresholdsLookup = CompoundThresholdData.ConvertToSearchMap(CompoundThresholds);
            }
        }

        private void ProcessFile(IOptions options, string rawFilePath)
        {
            Console.WriteLine("Processing file \"{0}\"", rawFilePath);
            using (var rawReader = new XCalDataReader(rawFilePath))
            {
                var results = rawReader.ReadRawData();
                if (results == null)
                {
                    return;
                }
                //Console.WriteLine("File \"{0}\": RawResults: {1}", rawFilePath, results.Count);
                var combined = rawReader.AggregateResults(results, options.DefaultThreshold, CompoundThresholdsLookup);
                //Console.WriteLine("File \"{0}\": CombinedResults: {1}", rawFilePath, combined.Count);

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

                var outputFilePath = Path.Combine(outputFolder, outputFileName);

                SrmCombinedResult.WriteCombinedResultsToFile(outputFilePath, combined);
            }
            Console.WriteLine("Finished Processing file \"{0}\"", rawFilePath);
        }
    }
}
