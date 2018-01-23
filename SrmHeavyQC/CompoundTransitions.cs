using System.Collections.Generic;

namespace SrmHeavyQC
{
    public class CompoundTransitions
    {
        public string CompoundName { get; }
        public double PrecursorMz { get; }
        public double StartTimeMinutes { get; }
        public double StopTimeMinutes { get; }
        public string Polarity { get; }
        public double CollisionVolts { get; }
        public List<SrmTableData> Transitions { get; }

        public CompoundTransitions(SrmTableData transition)
        {
            CompoundName = transition.CompoundName;
            PrecursorMz = transition.PrecursorMz;
            StartTimeMinutes = transition.StartTimeMinutes;
            StopTimeMinutes = transition.StopTimeMinutes;
            Polarity = transition.Polarity;
            CollisionVolts = transition.CollisionVolts;
            Transitions = new List<SrmTableData>();
        }
    }
}
