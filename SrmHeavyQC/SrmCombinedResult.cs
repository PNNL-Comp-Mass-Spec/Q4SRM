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

        public List<TransitionData> Transitions { get; }

        public SrmCombinedResult(TransitionData data)
        {
            CompoundName = data.CompoundName;
            PrecursorMz = data.PrecursorMz;
            StartTimeMinutes = data.StartTimeMinutes;
            StopTimeMinutes = data.StopTimeMinutes;
            TotalArea = 0;
            Transitions = new List<TransitionData>();
        }

        //public SrmCombinedResult()
        //{
        //    TotalArea = 0;
        //    Transitions = new List<TransitionData>();
        //    for (var i = 0; i < 50; i++)
        //    {
        //        Transitions.Add(new TransitionData());
        //    }
        //}

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
            TotalArea = Transitions.Sum(x => x.IntensitySum);

            if (TotalArea.Equals(0))
            {
                // Avoid divide by zero
                return;
            }

            foreach (var transition in Transitions)
            {
                transition.Ratio = transition.IntensitySum / TotalArea;
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

        public static void WriteCombinedResultsToFile(string filepath, List<SrmCombinedResult> results)
        {
            using (var csv = new CsvWriter(new StreamWriter(new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))))
            {
                var maxTrans = results.Max(x => x.Transitions.Count);
                csv.Configuration.RegisterClassMap(new SrmCombinedResultMap(maxTrans));
                csv.Configuration.Delimiter = "\t";
                csv.Configuration.Comment = '#';
                csv.Configuration.AllowComments = true;

                csv.WriteRecords(results);
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
                Map(x => x.TotalArea).Name("Summed Area").Index(index++);
                Map(x => x.PassesThreshold).Name("Passes Threshold").Index(index++);
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
