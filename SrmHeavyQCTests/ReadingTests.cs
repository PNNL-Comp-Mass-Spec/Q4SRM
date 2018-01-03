using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using SrmHeavyQC;

namespace SrmHeavyQCTests
{
    [TestFixture]
    public class ReadingTests
    {
        [Test]
        public void TestMethodVantage()
        {
            GetMethod(@"F:\SRM_data\Rush2_p14RR_62_26Oct17_Smeagol-WRUSHCol3_75x20a.raw");
        }

        [Test]
        public void TestMethodAltis()
        {
            GetMethod(@"F:\SRM_data\TdyQc1_Fnl_All_25Oct17_Balzac-W1sta1.raw");
        }

        [Test]
        public void TestProcessVantage()
        {
            ProcessTest(@"F:\SRM_data\Rush2_p14RR_62_26Oct17_Smeagol-WRUSHCol3_75x20a.raw", @"F:\SRM_data\Rush2_p14RR_62_26Oct17_Smeagol-WRUSHCol3_75x20a.tsv");
        }

        [Test]
        public void TestProcessAltis()
        {
            ProcessTest(@"F:\SRM_data\TdyQc1_Fnl_All_25Oct17_Balzac-W1sta1.raw", @"F:\SRM_data\TdyQc1_Fnl_All_25Oct17_Balzac-W1sta1.tsv");
        }

        private void ProcessTest(string rawPath, string resultPath)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            using (var reader = new XCalDataReader(rawPath))
            {
                sw.Stop();
                System.Console.WriteLine($"XCalDataReaderInitTime: {sw.Elapsed}");
                var results = reader.ReadRawData();
                //reader.OutputResultsToConsole(results);
                var comb = reader.AggregateResults(results);
                //reader.OutputResultsToConsole(comb);

                SrmCombinedResult.WriteCombinedResultsToFile(resultPath, comb);
            }
        }

        private void GetMethod(string rawPath)
        {
            using (var xcal = new XCalDataReader(rawPath))
            {
                var data = xcal.GetAllTransitions();
                foreach (var d in data.OrderBy(x => x.CompoundName))
                {
                    Console.WriteLine(d);
                }
            }
        }
    }
}
