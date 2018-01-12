using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;

namespace SrmHeavyQC
{
    public class CompoundThresholdData
    {
        public string CompoundName { get; set; }
        public double Threshold { get; set; }
        public double PrecursorMz { get; set; }

        public static Dictionary<string, CompoundThresholdData> ConvertToSearchMap(IEnumerable<CompoundThresholdData> thresholds)
        {
            var lookup = new Dictionary<string, CompoundThresholdData>();
            foreach (var t in thresholds)
            {
                if (!lookup.ContainsKey(t.CompoundName))
                {
                    lookup[t.CompoundName] = t;
                }
                else
                {
                    lookup.Add(t.CompoundName, t);
                }
            }

            return lookup;
        }

        public static IEnumerable<CompoundThresholdData> ReadFromFile(string filePath)
        {
            using (var csv = new CsvReader(new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))))
            {
                csv.Configuration.RegisterClassMap(new CompoundThresholdDataMap());
                csv.Configuration.Delimiter = "\t";
                csv.Configuration.PrepareHeaderForMatch = header => header?.ToLower().Trim();
                csv.Configuration.MissingFieldFound = null; // Allow missing fields
                csv.Configuration.HeaderValidated = null; // Allow missing header items
                csv.Configuration.Comment = '#';
                csv.Configuration.AllowComments = true;

                foreach (var record in csv.GetRecords<CompoundThresholdData>())
                {
                    yield return record;
                }
            }
        }

        public sealed class CompoundThresholdDataMap : ClassMap<CompoundThresholdData>
        {
            public CompoundThresholdDataMap()
            {
                Map(x => x.PrecursorMz).Name("Precursor (m/z)", "Precursor", "Parent").TypeConverterOption.NumberStyles(NumberStyles.Float | NumberStyles.AllowThousands);
                Map(x => x.Threshold).Name("Threshold", "Product").TypeConverterOption.NumberStyles(NumberStyles.Float | NumberStyles.AllowThousands);
                Map(x => x.CompoundName).Name("Compound Name", "Name");
            }
        }
    }
}
