using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;

namespace SrmHeavyQC
{
    public class SrmCombinedResult
    {
        public string CompoundName { get; }
        public double PrecursorMz { get; }
        public double StartTimeMinutes { get; }
        public double StopTimeMinutes { get; }

        public double TotalIntensitySum { get; private set; }
        public double MaxIntensity { get; private set; }
        public double IntensityRatioMaxVsMedian { get; private set; }
        public double MedianIntensity { get; private set; }

        /// <summary>
        /// The time of the max intensity, normalized to the compound elution time window
        /// </summary>
        public double MaxIntensityNet { get; private set; }

        public double Threshold { get; set; }

        public bool PassesThreshold
        {
            get { return TotalIntensitySum >= Threshold; }
        }

        public List<TransitionData> Transitions { get; }

        public SrmCombinedResult(TransitionData data)
        {
            CompoundName = data.CompoundName;
            PrecursorMz = data.PrecursorMz;
            StartTimeMinutes = data.StartTimeMinutes;
            StopTimeMinutes = data.StopTimeMinutes;
            TotalIntensitySum = 0;
            Transitions = new List<TransitionData>();
        }

        public SrmCombinedResult()
        {
            Transitions = new List<TransitionData>();
            for (var i = 0; i < 10; i++)
            {
                Transitions.Add(new TransitionData());
            }
        }

        public void AddTransition(TransitionData result)
        {
            if (result == null)
            {
                return;
            }

            if (Transitions.Count >= 10)
            {
                System.Console.WriteLine(@"Warning: Will not output additional transition for compound ""{0}"": it already has 10 transitions.", CompoundName);
            }

            if (!Transitions.Contains(result))
            {
                Transitions.Add(result);
            }

            CalculateSummaryData();
        }

        private void CalculateSummaryData()
        {
            TotalIntensitySum = Transitions.Sum(x => x.IntensitySum);

            if (TotalIntensitySum.Equals(0))
            {
                // Avoid divide by zero
                return;
            }

            foreach (var transition in Transitions)
            {
                transition.RatioOfCompoundTotalIntensity = transition.IntensitySum / TotalIntensitySum;
            }

            var maxIntTrans = Transitions.OrderByDescending(x => x.MaxIntensity).First();
            MaxIntensity = maxIntTrans.MaxIntensity;
            MaxIntensityNet = (maxIntTrans.MaxIntensityTime - maxIntTrans.StartTimeMinutes) / (maxIntTrans.StopTimeMinutes - maxIntTrans.StartTimeMinutes);
            var fullIntensityList = new List<double>();
            foreach (var result in Transitions)
            {
                fullIntensityList.AddRange(result.Intensities);
            }
            fullIntensityList.Sort();
            if (fullIntensityList.Count % 2 == 0)
            {
                // even number of items, must average the middle 2
                var int1 = fullIntensityList[fullIntensityList.Count / 2];
                var int2 = fullIntensityList[fullIntensityList.Count / 2 + 1];
                MedianIntensity = (int1 + int2) / 2.0;
            }
            else
            {
                // odd number of items, integer division will give us the center index
                MedianIntensity = fullIntensityList[fullIntensityList.Count / 2];
            }

            IntensityRatioMaxVsMedian = MaxIntensity / MedianIntensity;
            if (double.IsInfinity(IntensityRatioMaxVsMedian) || IntensityRatioMaxVsMedian > MaxIntensity)
            {
                //System.Console.WriteLine("InfinityEncountered!: {0} MaxInt: {1} Median {2}");
                IntensityRatioMaxVsMedian = MaxIntensity;
            }
        }

        public TransitionData Transition01 => Transitions.Count >= 1 ? Transitions[0] : new TransitionData();
        public TransitionData Transition02 => Transitions.Count >= 2 ? Transitions[1] : new TransitionData();
        public TransitionData Transition03 => Transitions.Count >= 3 ? Transitions[2] : new TransitionData();
        public TransitionData Transition04 => Transitions.Count >= 4 ? Transitions[3] : new TransitionData();
        public TransitionData Transition05 => Transitions.Count >= 5 ? Transitions[4] : new TransitionData();
        public TransitionData Transition06 => Transitions.Count >= 6 ? Transitions[5] : new TransitionData();
        public TransitionData Transition07 => Transitions.Count >= 7 ? Transitions[6] : new TransitionData();
        public TransitionData Transition08 => Transitions.Count >= 8 ? Transitions[7] : new TransitionData();
        public TransitionData Transition09 => Transitions.Count >= 9 ? Transitions[8] : new TransitionData();
        public TransitionData Transition10 => Transitions.Count >= 10 ? Transitions[9] : new TransitionData();

        public static bool CheckSettings(string filepath, ISettingsData currentSettings)
        {
            if (string.IsNullOrWhiteSpace(filepath) || !File.Exists(filepath))
            {
                return false;
            }

            var fileSettings = new SettingsData();

            using (var streamReader = new StreamReader(new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                var line = streamReader.ReadLine();
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("#"))
                {
                    return false;
                }

                fileSettings.PopulateFromTsvComment(line);
            }

            return currentSettings.SettingsEquals(fileSettings);
        }

        public static void WriteCombinedResultsToFile(string filepath, List<SrmCombinedResult> results, ISettingsData settings)
        {
            using (var csv = new CsvWriter(new StreamWriter(new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))))
            {
                var maxTrans = results.Max(x => x.Transitions.Count);
                csv.Configuration.RegisterClassMap(new SrmCombinedResultMap(maxTrans));
                csv.Configuration.Delimiter = "\t";
                csv.Configuration.Comment = '#';
                csv.Configuration.AllowComments = true;

                // Write the settings comment
                csv.WriteComment(" Settings: " + settings.ConvertToTsvComment());
                // Make sure to finish the line
                csv.NextRecord();
                // Write everything else, including the headers.
                csv.WriteRecords(results);
            }
        }

        public static IEnumerable<SrmCombinedResult> ReadCombinedResultsFile(string filepath)
        {
            using (var csv = new CsvReader(new StreamReader(new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))))
            {
                // TODO: read the comment, and parse it into a settings object?
                // TODO: Could read the header, and determine the appropriate map size to use.
                csv.Configuration.RegisterClassMap(new SrmCombinedResultMap(10));
                csv.Configuration.Delimiter = "\t";
                csv.Configuration.PrepareHeaderForMatch = header => header?.ToLower().Trim();
                csv.Configuration.MissingFieldFound = null; // Allow missing fields
                csv.Configuration.HeaderValidated = null; // Allow missing header items
                csv.Configuration.Comment = '#';
                csv.Configuration.AllowComments = true;

                foreach (var result in csv.GetRecords<SrmCombinedResult>())
                {
                    yield return result;
                }
            }
        }

        public class SrmCombinedResultMap : ClassMap<SrmCombinedResult>
        {
            public SrmCombinedResultMap() : this(5)
            {
            }

            public SrmCombinedResultMap(int maxTransitionCount)
            {
                var index = 0;
                Map(x => x.CompoundName).Name("Compound Name").Index(index++);
                Map(x => x.PrecursorMz).Name("Precursor m/z").Index(index++);
                Map(x => x.StartTimeMinutes).Name("Start Time (min)").Index(index++);
                Map(x => x.StopTimeMinutes).Name("Stop Time (min)").Index(index++);
                Map(x => x.TotalIntensitySum).Name("TotalIntensity").Index(index++).TypeConverter<DecimalLimitingDoubleTypeConverter>();
                Map(x => x.PassesThreshold).Name("Passes Threshold").Index(index++);
                Map(x => x.MaxIntensity).Name("IntensityMax").Index(index++).TypeConverter<DecimalLimitingDoubleTypeConverter>();
                Map(x => x.MaxIntensityNet).Name("IntensityMaxNET").Index(index++).TypeConverter<DecimalLimitingDoubleTypeConverter>();
                Map(x => x.IntensityRatioMaxVsMedian).Name("IntensityRatioMaxVsMedian").Index(index++).TypeConverter<DecimalLimitingDoubleTypeConverter>();
                Map(x => x.MedianIntensity).Name("IntensityMedian").Index(index++).TypeConverter<DecimalLimitingDoubleTypeConverter>();
                /*/
                for (var i = 0; i < maxTransitionCount; i++)
                {
                    // Doesn't work due to NullReferenceExceptions when trying to map to object member
                    var localI = i;
                    References<TransitionData.TransitionSummaryDataMap>(x => x.Transitions[localI]).Prefix($"Trans_{localI + 1} ");
                }
                /*/
                if (maxTransitionCount >= 1)
                {
                    References<TransitionData.TransitionSummaryDataMap>(x => x.Transition01).Prefix("Trans_1 ");
                }
                if (maxTransitionCount >= 2)
                {
                    References<TransitionData.TransitionSummaryDataMap>(x => x.Transition02).Prefix("Trans_2 ");
                }
                if (maxTransitionCount >= 3)
                {
                    References<TransitionData.TransitionSummaryDataMap>(x => x.Transition03).Prefix("Trans_3 ");
                }
                if (maxTransitionCount >= 4)
                {
                    References<TransitionData.TransitionSummaryDataMap>(x => x.Transition04).Prefix("Trans_4 ");
                }
                if (maxTransitionCount >= 5)
                {
                    References<TransitionData.TransitionSummaryDataMap>(x => x.Transition05).Prefix("Trans_5 ");
                }
                if (maxTransitionCount >= 6)
                {
                    References<TransitionData.TransitionSummaryDataMap>(x => x.Transition06).Prefix("Trans_6 ");
                }
                if (maxTransitionCount >= 7)
                {
                    References<TransitionData.TransitionSummaryDataMap>(x => x.Transition07).Prefix("Trans_7 ");
                }
                if (maxTransitionCount >= 8)
                {
                    References<TransitionData.TransitionSummaryDataMap>(x => x.Transition08).Prefix("Trans_8 ");
                }
                if (maxTransitionCount >= 9)
                {
                    References<TransitionData.TransitionSummaryDataMap>(x => x.Transition09).Prefix("Trans_9 ");
                }
                if (maxTransitionCount >= 10)
                {
                    References<TransitionData.TransitionSummaryDataMap>(x => x.Transition10).Prefix("Trans_10 ");
                }
                /**/
            }
        }
    }
}
