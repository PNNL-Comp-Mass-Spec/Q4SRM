using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;

namespace Q4SRM.Data
{
    public class SummaryStats
    {
        public string DatasetName { get; }
        public int PeptideCount { get; }
        public int PeptideCountPassing { get; }
        public double TotalIntensityAvg { get; }
        public double TotalIntensityMedian { get; }

        public SummaryStats(string datasetName, List<CompoundData> datasetResults)
        {
            DatasetName = datasetName;
            PeptideCount = datasetResults.Count;
            PeptideCountPassing = datasetResults.Count(x => x.PassesAllThresholds);
            TotalIntensityAvg = datasetResults.Average(x => x.TotalIntensitySum);
            TotalIntensityMedian = datasetResults.Median(x => x.TotalIntensitySum);
        }

        public static void WriteToFile(string filePath, IEnumerable<SummaryStats> thresholds)
        {
            using (var csv = new CsvWriter(new StreamWriter(new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))))
            {
                csv.Configuration.RegisterClassMap(new CompoundThresholdDataMap());
                csv.Configuration.Delimiter = "\t";
                csv.Configuration.Comment = '#';
                csv.Configuration.AllowComments = true;

                csv.WriteRecords(thresholds);
            }
        }

        public sealed class CompoundThresholdDataMap : ClassMap<SummaryStats>
        {
            public CompoundThresholdDataMap()
            {
                var index = 0;
                Map(x => x.DatasetName).Name("Dataset").Index(index++);
                Map(x => x.PeptideCount).Name("PeptideCount").Index(index++);
                Map(x => x.PeptideCountPassing).Name("PeptideCountPassing").Index(index++);
                Map(x => x.TotalIntensityAvg).Name("TotalIntensityAvg").Index(index++);
                Map(x => x.TotalIntensityMedian).Name("TotalIntensityMedian").Index(index++);
            }
        }
    }
}
