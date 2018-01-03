using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.Configuration;

namespace SrmHeavyQC
{
    public class TransitionSummaryData
    {
        public double ProductMz { get; }
        public double Area { get; }
        public double Ratio { get; }

        public TransitionSummaryData(double mz, double area, double ratio)
        {
            ProductMz = mz;
            Area = area;
            Ratio = ratio;
        }

        public TransitionSummaryData()
        {
        }

        public sealed class TransitionSummaryDataMap : ClassMap<TransitionSummaryData>
        {
            public TransitionSummaryDataMap()
            {
                Map(x => x.ProductMz).Name("m/z");
                Map(x => x.Area).Name("area");
                Map(x => x.Ratio).Name("ratio");
            }
        }
    }
}
