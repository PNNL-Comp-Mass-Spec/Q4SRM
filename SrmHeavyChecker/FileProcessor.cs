using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SrmHeavyQC;

namespace SrmHeavyChecker
{
    public class FileProcessor
    {
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

        private void ProcessFile(IOptions options, string rawFilePath)
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

            var outputFilePath = Path.Combine(outputFolder, outputFileName);

            if (!options.OverwriteOutput && File.Exists(outputFilePath) && CompoundData.CheckSettings(outputFilePath, options))
            {
                Console.WriteLine("Skipping file \"{0}\"; existing output was created with matching settings", rawFilePath);
                return;
            }

            Console.WriteLine("Processing file \"{0}\"", rawFilePath);
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
            }
            Console.WriteLine("Finished Processing file \"{0}\"", rawFilePath);
        }
    }
}
