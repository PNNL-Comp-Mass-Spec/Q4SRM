using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;

namespace SrmHeavyQC
{
    public class TransitionData
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

        public double MaxIntensity { get; set; }
        public double MaxIntensityTime { get; set; }
        public double MedianIntensity { get; private set; }
        public double MaxIntensityVsMedian { get; private set; }
        public double MaxIntensityNET { get; private set; }
        public bool PassesNET { get; private set; }
        public double IntensitySum { get; set; }
        public double RatioOfCompoundTotalIntensity { get; private set; }

        public struct DataPoint
        {
            public int Scan { get; }
            public double Time { get; }
            public double Intensity { get; }

            public DataPoint(int scan, double time, double intensity)
            {
                Scan = scan;
                Time = time;
                Intensity = intensity;
            }
        }

        public List<DataPoint> Intensities { get; } = new List<DataPoint>();

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

        public void CalculateStats(double compoundTotalIntensitySum, double edgeNetThresholdMinutes)
        {
            MedianIntensity = Intensities.Median(x => x.Intensity);
            MaxIntensityVsMedian = MaxIntensity / MedianIntensity;
            MaxIntensityNET = (MaxIntensityTime - StartTimeMinutes) / (StopTimeMinutes - StartTimeMinutes);
            var edgeNetThreshold = (edgeNetThresholdMinutes) / (StopTimeMinutes - StartTimeMinutes);
            PassesNET = edgeNetThreshold <= MaxIntensityNET && MaxIntensityNET <= 1 - edgeNetThreshold;
            RatioOfCompoundTotalIntensity = IntensitySum / compoundTotalIntensitySum;
        }

        public override string ToString()
        {
            return $"{CompoundName,-50} {PrecursorMz,10:F2} {ProductMz,10:F2} {StartTimeMinutes,10:F2} {StopTimeMinutes,10:F2}";
        }

        public static IEnumerable<TransitionData> ParseSrmTable(Stream stream)
        {
            using (var csv = new CsvReader(new StreamReader(stream)))
            {
                csv.Configuration.RegisterClassMap(new MethodParsingMap());
                csv.Configuration.Delimiter = "\t";
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

        public sealed class MethodParsingMap : ClassMap<TransitionData>
        {
            public MethodParsingMap()
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

        public sealed class TransitionSummaryDataMap : ClassMap<TransitionData>
        {
            public TransitionSummaryDataMap()
            {
                Map(x => x.ProductMz).Name("m/z");
                Map(x => x.IntensitySum).Name("totalIntensity").TypeConverter<DecimalLimitingDoubleTypeConverter>();
                Map(x => x.RatioOfCompoundTotalIntensity).Name("ratio").TypeConverter<DecimalLimitingDoubleTypeConverter>();
                Map(x => x.MaxIntensity).Name("Max Intensity").TypeConverter<DecimalLimitingDoubleTypeConverter>();
                Map(x => x.MaxIntensityNET).Name("Peak Position (NET)").TypeConverter<DecimalLimitingDoubleTypeConverter>();
                Map(x => x.MedianIntensity).Name("Median Intensity").TypeConverter<DecimalLimitingDoubleTypeConverter>();
                Map(x => x.MaxIntensityVsMedian).Name("MaxVsMedian Intensity").TypeConverter<DecimalLimitingDoubleTypeConverter>();
            }
        }
    }
}
