using System;
using System.Collections.Generic;
using System.Linq;
using Q4SRM.DataReaders;
using Q4SRM.Settings;

namespace Q4SRM.Data
{
    public static class ExtensionMethods
    {
        public static double Median(this IEnumerable<double> data)
        {
            if (data == null)
            {
                return 0;
            }
            var list = data.ToList();
            if (list.Count == 0)
            {
                return 0;
            }

            list.Sort();

            // Integer division
            var mid = list.Count / 2;
            if (list.Count % 2 == 0)
            {
                // even number of items, must average the middle 2
                var int1 = list[mid - 1];
                var int2 = list[mid];
                return (int1 + int2) / 2.0;
            }

            // odd number of items, integer division will give us the center index
            return list[mid];
        }

        public static double Median<T>(this IEnumerable<T> data, Func<T, double> selector)
        {
            return data.Select(selector).Median();
        }

        public static List<CompoundData> ReadMethodData(this IMethodReader reader, ISettingsData settings)
        {
            var transitions = reader.GetAllTransitions();

            if (!settings.CheckAllCompounds)
            {
                transitions = transitions.GetHeavyTransitions();
            }

            //Console.WriteLine($"File \"{reader.DatasetPath}\": {transitions.Count} heavy transitions in instrument method.");
            if (transitions.Count == 0)
            {
                var heavy = settings.CheckAllCompounds ? "" : " heavy";
                Console.WriteLine($"ERROR: Could not read instrument methods or find{heavy} transitions for file \"{reader.DatasetPath}\"");
                return null;
            }
            // TODO: Could determine heavy peptides by checking names vs. precursor m/z?
            return transitions.JoinTransitions(settings);
        }

        /// <summary>
        /// Get the heavy transitions from the transition data
        /// </summary>
        /// <returns></returns>
        public static List<TransitionData> GetHeavyTransitions(this List<TransitionData> allTransitions)
        {
            // TODO: Check for heavy transitions using precursor mass difference 0.5 <= m/z <= 6.0
            return allTransitions.Where(x => x.IsHeavy).ToList();
        }

        public static List<CompoundData> JoinTransitions(this List<TransitionData> transitions, ISettingsData settings)
        {
            var compoundThresholds = settings.LoadCompoundThresholds();
            if (compoundThresholds == null)
            {
                compoundThresholds = new Dictionary<string, CompoundThresholdData>();
            }
            var combined = new Dictionary<string, CompoundData>();
            foreach (var item in transitions)
            {
                if (!combined.TryGetValue(item.CompoundName, out var group))
                {
                    group = new CompoundData(item)
                    {
                        IntensityThreshold = settings.DefaultIntensityThreshold,
                        EdgeNETThresholdMinutes = settings.EdgeNETThresholdMinutes,
                        ElutionConcurrenceThresholdMinutes = settings.ElutionConcurrenceThresholdMinutes,
                        SignalToNoiseHeuristicThreshold = settings.SignalToNoiseHeuristicThreshold
                    };
                    // Look for and handle custom thresholds
                    if (compoundThresholds.TryGetValue(item.CompoundName, out var cThreshold))
                    {
                        group.IntensityThreshold = cThreshold.Threshold;
                    }
                    combined.Add(group.CompoundName, group);
                }
                group.Transitions.Add(item);
            }

            return combined.Values.ToList();
        }
    }
}
