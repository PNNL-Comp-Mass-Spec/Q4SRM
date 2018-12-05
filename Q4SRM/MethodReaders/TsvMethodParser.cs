using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using Q4SRM.Data;
using Q4SRM.DataReaders;

namespace Q4SRM.MethodReaders
{
    public class TsvMethodParser : IMethodReader
    {
        public string DatasetPath { get; }

        public TsvMethodParser(string datasetPath)
        {
            DatasetPath = datasetPath;
        }

        public List<TransitionData> GetAllTransitions()
        {
            return ParseTransitions(DatasetPath).ToList();
        }

        public static IEnumerable<TransitionData> ParseTransitions(string filePath)
        {
            using (var csv = new CsvReader(new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))))
            {
                csv.Configuration.RegisterClassMap(new TransitionData.MethodParsingMap());
                if (!filePath.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
                {
                    csv.Configuration.Delimiter = "\t";
                }
                csv.Configuration.PrepareHeaderForMatch = header => header?.ToLower().Trim();
                csv.Configuration.MissingFieldFound = null; // Allow missing fields
                csv.Configuration.HeaderValidated = null; // Allow missing header items
                csv.Configuration.Comment = '#';
                csv.Configuration.AllowComments = true;

                foreach (var record in csv.GetRecords<TransitionData>())
                {
                    yield return record;
                }
            }
        }

        public sealed class TsvMethodParsingMap : ClassMap<TransitionData>
        {
            public TsvMethodParsingMap()
            {
                Map(x => x.PrecursorMz).Name("Precursor (m/z)", "Parent").TypeConverterOption.NumberStyles(NumberStyles.Float | NumberStyles.AllowThousands);
                Map(x => x.ProductMz).Name("Product (m/z)", "Product").TypeConverterOption.NumberStyles(NumberStyles.Float | NumberStyles.AllowThousands);
                Map(x => x.StartTimeMinutes).Name("Start Time (min)", "Start").TypeConverterOption.NumberStyles(NumberStyles.Float | NumberStyles.AllowThousands);
                Map(x => x.StopTimeMinutes).Name("End Time (min)", "Stop").TypeConverterOption.NumberStyles(NumberStyles.Float | NumberStyles.AllowThousands);
                Map(x => x.CompoundName).Name("Compound Name", "Name");
            }
        }
    }
}
