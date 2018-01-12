using System;
using System.IO;
using System.Threading.Tasks;
using PRISM;
using SrmHeavyQC;

namespace SrmHeavyChecker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var parser = new CommandLineParser<Options>();
            var parsed = parser.ParseArgs(args);
            var options = parsed.ParsedResults;

            if (!parsed.Success || !options.Validate())
            {
                return;
            }

            RunProcessing(options);
        }

        public static void RunProcessing(Options options)
        {
            if (options.MaxThreads == 1 || options.FilesToProcess.Count == 1)
            {
                foreach (var file in options.FilesToProcess)
                {
                    ProcessFile(options, file);
                }
            }
            else
            {
                var parallelOptions = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = options.MaxThreads,
                };
                Parallel.ForEach(options.FilesToProcess, parallelOptions, x => ProcessFile(options, x));
            }
        }

        private static void ProcessFile(Options options, string rawFilePath)
        {
            Console.WriteLine("Processing file \"{0}\"", rawFilePath);
            using (var rawReader = new XCalDataReader(rawFilePath))
            {
                var results = rawReader.ReadRawData(options.PpmTolerance);
                if (results == null)
                {
                    return;
                }
                //Console.WriteLine("File \"{0}\": RawResults: {1}", rawFilePath, results.Count);
                var combined = rawReader.AggregateResults(results, options.DefaultThreshold, options.CompoundThresholdsLookup);
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
