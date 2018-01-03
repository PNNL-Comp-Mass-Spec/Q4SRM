using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;

namespace SrmHeavyQC
{
    public class SrmTableData
    {
        //Parent	Product	CE	Start	Stop	Pol	Trigger	Reference	Name
        //Compound Name	Start Time (min)	End Time (min)	Polarity	Precursor (m/z)	Product (m/z)	Collision Energy (V)
        private string compoundName = "";

        public double PrecursorMz { get; set; }
        public double ProductMz { get; set; }
        public double CollisionVolts { get; set; }
        public double StartTimeMinutes { get; set; }
        public double StopTimeMinutes { get; set; }
        public string Polarity { get; set; }
        public double Trigger { get; set; }
        public string Reference { get; set; }

        public string CompoundName
        {
            get { return compoundName; }
            set
            {
                compoundName = value;
                IsHeavy = false;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var lower = value.ToLower();
                    if (lower.Contains("heavy") || lower.Contains("hvy"))
                    {
                        IsHeavy = true;
                    }
                }
            }
        }

        public bool IsHeavy { get; private set; }

        public override string ToString()
        {
            return $"{CompoundName,-50} {PrecursorMz,10:F2} {ProductMz,10:F2} {StartTimeMinutes,10:F2} {StopTimeMinutes,10:F2}";
        }

        public static IEnumerable<SrmTableData> ParseSrmTable(Stream stream)
        {
            using (var csv = new CsvReader(new StreamReader(stream)))
            {
                csv.Configuration.RegisterClassMap(new SrmTableDataMap());
                csv.Configuration.Delimiter = "\t";
                csv.Configuration.PrepareHeaderForMatch = header => header?.Trim();
                csv.Configuration.MissingFieldFound = null; // Allow missing fields
                csv.Configuration.HeaderValidated = null; // Allow missing header items
                csv.Configuration.Comment = '#';
                csv.Configuration.AllowComments = true;

                foreach (var record in csv.GetRecords<SrmTableData>())
                {
                    yield return record;
                }
            }
        }

        public sealed class SrmTableDataMap : ClassMap<SrmTableData>
        {
            public SrmTableDataMap()
            {
                //TSQ Vantage: Parent	Product	CE	Start	Stop	Pol	Trigger	Reference	Name
                //TSQ Altis:   Compound Name	Start Time (min)	End Time (min)	Polarity	Precursor (m/z)	Product (m/z)	Collision Energy (V)
                // Using a custom number style, since (at least on the Altis) they use a thousands separator
                Map(x => x.PrecursorMz).Name("Precursor (m/z)", "Parent").TypeConverterOption.NumberStyles(NumberStyles.Float | NumberStyles.AllowThousands);
                Map(x => x.ProductMz).Name("Product (m/z)", "Product").TypeConverterOption.NumberStyles(NumberStyles.Float | NumberStyles.AllowThousands);
                Map(x => x.CollisionVolts).Name("Collision Energy (V)", "CE").TypeConverterOption.NumberStyles(NumberStyles.Float | NumberStyles.AllowThousands);
                Map(x => x.StartTimeMinutes).Name("Start Time (min)", "Start").TypeConverterOption.NumberStyles(NumberStyles.Float | NumberStyles.AllowThousands);
                Map(x => x.StopTimeMinutes).Name("End Time (min)", "Stop").TypeConverterOption.NumberStyles(NumberStyles.Float | NumberStyles.AllowThousands);
                Map(x => x.Polarity).Name("Polarity", "Pol");
                Map(x => x.Trigger).Name("Trigger").Default(-1);
                Map(x => x.Reference).Name("Reference").Default("");
                Map(x => x.CompoundName).Name("Compound Name", "Name");
            }
        }
    }
}
