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

        public double TotalArea { get; private set; }

        public double Threshold { get; set; }

        public bool PassesThreshold
        {
            get { return TotalArea >= Threshold; }
        }

        public List<TransitionData> TransitionResults { get; }
        public List<TransitionSummaryData> TransitionSummaries { get; }

        public SrmCombinedResult(TransitionData data)
        {
            CompoundName = data.CompoundName;
            PrecursorMz = data.PrecursorMz;
            StartTimeMinutes = data.StartTimeMinutes;
            StopTimeMinutes = data.StopTimeMinutes;
            TotalArea = 0;
            TransitionResults = new List<TransitionData>();
            TransitionSummaries = new List<TransitionSummaryData>();
        }

        //public SrmCombinedResult()
        //{
        //    TotalArea = 0;
        //    TransitionResults = new List<SrmResult>();
        //    TransitionSummaries = new List<TransitionSummaryData>();
        //    for (var i = 0; i < 50; i++)
        //    {
        //        TransitionSummaries.Add(new TransitionSummaryData());
        //    }
        //}

        public void AddTransition(TransitionData result)
        {
            if (result == null)
            {
                return;
            }

            if (TransitionResults.Count >= 10)
            {
                System.Console.WriteLine(@"Warning: Will not output additional transition for compound ""{0}"": it already has 10 transitions.", CompoundName);
            }

            if (!TransitionResults.Contains(result))
            {
                TransitionResults.Add(result);
            }

            CalculateSummaryData();
        }

        private void CalculateSummaryData()
        {
            TotalArea = TransitionResults.Sum(x => x.IntensitySum);

            if (TotalArea.Equals(0))
            {
                // Avoid divide by zero
                return;
            }

            TransitionSummaries.Clear();
            TransitionSummaries.AddRange(TransitionResults.Select(x => new TransitionSummaryData(x.ProductMz, x.IntensitySum, x.IntensitySum / TotalArea)));
        }

        public TransitionSummaryData Transition1Summary => TransitionSummaries.Count >= 1 ? TransitionSummaries[0] : new TransitionSummaryData();
        public TransitionSummaryData Transition2Summary => TransitionSummaries.Count >= 2 ? TransitionSummaries[1] : new TransitionSummaryData();
        public TransitionSummaryData Transition3Summary => TransitionSummaries.Count >= 3 ? TransitionSummaries[2] : new TransitionSummaryData();
        public TransitionSummaryData Transition4Summary => TransitionSummaries.Count >= 4 ? TransitionSummaries[3] : new TransitionSummaryData();
        public TransitionSummaryData Transition5Summary => TransitionSummaries.Count >= 5 ? TransitionSummaries[4] : new TransitionSummaryData();
        public TransitionSummaryData Transition6Summary => TransitionSummaries.Count >= 6 ? TransitionSummaries[5] : new TransitionSummaryData();
        public TransitionSummaryData Transition7Summary => TransitionSummaries.Count >= 7 ? TransitionSummaries[6] : new TransitionSummaryData();
        public TransitionSummaryData Transition8Summary => TransitionSummaries.Count >= 8 ? TransitionSummaries[7] : new TransitionSummaryData();
        public TransitionSummaryData Transition9Summary => TransitionSummaries.Count >= 9 ? TransitionSummaries[8] : new TransitionSummaryData();
        public TransitionSummaryData Transition10Summary => TransitionSummaries.Count >= 10 ? TransitionSummaries[9] : new TransitionSummaryData();

        public static void WriteCombinedResultsToFile(string filepath, List<SrmCombinedResult> results)
        {
            using (var csv = new CsvWriter(new StreamWriter(new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))))
            {
                var maxTrans = results.Max(x => x.TransitionSummaries.Count);
                csv.Configuration.RegisterClassMap(new SrmCombinedResultMap(maxTrans));
                csv.Configuration.Delimiter = "\t";
                csv.Configuration.Comment = '#';
                csv.Configuration.AllowComments = true;

                csv.WriteRecords(results);
            }
        }

        public class SrmCombinedResultMap : ClassMap<SrmCombinedResult>
        {
            public SrmCombinedResultMap()
            {
                var index = 0;
                Map(x => x.CompoundName).Name("Compound Name").Index(index++);
                Map(x => x.PrecursorMz).Name("Precursor m/z").Index(index++);
                Map(x => x.StartTimeMinutes).Name("Start Time (min)").Index(index++);
                Map(x => x.StopTimeMinutes).Name("Stop Time (min)").Index(index++);
                Map(x => x.TotalArea).Name("Summed Area").Index(index++);
                Map(x => x.PassesThreshold).Name("Passes Threshold").Index(index++);
                //Map(x => x.Transition1.Transition.ProductMz).Name("Trans_1 m/z").Index(index++);
                //Map(x => x.Transition1.Area).Name("Trans_1 area").Index(index++);
                //Map(x => x.Transition1Ratio).Name("Trans_1 ratio").Index(index++);
                //Map(x => x.Transition2.Transition.ProductMz).Name("Trans_2 m/z").Index(index++);
                //Map(x => x.Transition2.Area).Name("Trans_2 area").Index(index++);
                //Map(x => x.Transition2Ratio).Name("Trans_2 ratio").Index(index++);
                //Map(x => x.Transition3.Transition.ProductMz).Name("Trans_3 m/z").Index(index++);
                //Map(x => x.Transition3.Area).Name("Trans_3 area").Index(index++);
                //Map(x => x.Transition3Ratio).Name("Trans_3 ratio").Index(index++);
                //Map(x => x.Transition4.Transition.ProductMz).Name("Trans_4 m/z").Index(index++);
                //Map(x => x.Transition4.Area).Name("Trans_4 area").Index(index++);
                //Map(x => x.Transition4Ratio).Name("Trans_4 ratio").Index(index++);
                //Map(x => x.Transition5.Transition.ProductMz).Name("Trans_5 m/z").Index(index++);
                //Map(x => x.Transition5.Area).Name("Trans_5 area").Index(index++);
                //Map(x => x.Transition5Ratio).Name("Trans_5 ratio").Index(index++);

                References<TransitionSummaryData.TransitionSummaryDataMap>(x => x.Transition1Summary).Prefix("Trans_1 ");
                References<TransitionSummaryData.TransitionSummaryDataMap>(x => x.Transition2Summary).Prefix("Trans_2 ");
                References<TransitionSummaryData.TransitionSummaryDataMap>(x => x.Transition3Summary).Prefix("Trans_3 ");
                References<TransitionSummaryData.TransitionSummaryDataMap>(x => x.Transition4Summary).Prefix("Trans_4 ");
                References<TransitionSummaryData.TransitionSummaryDataMap>(x => x.Transition5Summary).Prefix("Trans_5 ");
            }

            public SrmCombinedResultMap(int maxTransitionCount)
            {
                var index = 0;
                Map(x => x.CompoundName).Name("Compound Name").Index(index++);
                Map(x => x.PrecursorMz).Name("Precursor m/z").Index(index++);
                Map(x => x.StartTimeMinutes).Name("Start Time (min)").Index(index++);
                Map(x => x.StopTimeMinutes).Name("Stop Time (min)").Index(index++);
                Map(x => x.TotalArea).Name("Summed Area").Index(index++);
                Map(x => x.PassesThreshold).Name("Passes Threshold").Index(index++);
                /*/
                for (var i = 0; i < maxTransitionCount; i++)
                {
                    // Doesn't work due to NullReferenceExceptions when trying to map to object member
                    var localI = i;
                    References<TransitionSummaryData.TransitionSummaryDataMap>(x => x.TransitionSummaries[localI]).Prefix($"Trans_{localI + 1} ");
                }
                /*/
                if (maxTransitionCount >= 1)
                {
                    References<TransitionSummaryData.TransitionSummaryDataMap>(x => x.Transition1Summary).Prefix("Trans_1 ");
                }
                if (maxTransitionCount >= 2)
                {
                    References<TransitionSummaryData.TransitionSummaryDataMap>(x => x.Transition2Summary).Prefix("Trans_2 ");
                }
                if (maxTransitionCount >= 3)
                {
                    References<TransitionSummaryData.TransitionSummaryDataMap>(x => x.Transition3Summary).Prefix("Trans_3 ");
                }
                if (maxTransitionCount >= 4)
                {
                    References<TransitionSummaryData.TransitionSummaryDataMap>(x => x.Transition4Summary).Prefix("Trans_4 ");
                }
                if (maxTransitionCount >= 5)
                {
                    References<TransitionSummaryData.TransitionSummaryDataMap>(x => x.Transition5Summary).Prefix("Trans_5 ");
                }
                if (maxTransitionCount >= 6)
                {
                    References<TransitionSummaryData.TransitionSummaryDataMap>(x => x.Transition6Summary).Prefix("Trans_6 ");
                }
                if (maxTransitionCount >= 7)
                {
                    References<TransitionSummaryData.TransitionSummaryDataMap>(x => x.Transition7Summary).Prefix("Trans_7 ");
                }
                if (maxTransitionCount >= 8)
                {
                    References<TransitionSummaryData.TransitionSummaryDataMap>(x => x.Transition8Summary).Prefix("Trans_8 ");
                }
                if (maxTransitionCount >= 9)
                {
                    References<TransitionSummaryData.TransitionSummaryDataMap>(x => x.Transition9Summary).Prefix("Trans_9 ");
                }
                if (maxTransitionCount >= 10)
                {
                    References<TransitionSummaryData.TransitionSummaryDataMap>(x => x.Transition10Summary).Prefix("Trans_10 ");
                }
                /**/
            }
        }
    }
}
