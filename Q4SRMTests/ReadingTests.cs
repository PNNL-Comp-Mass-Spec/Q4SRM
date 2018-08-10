using System;
using System.Linq;
using NUnit.Framework;
using Q4SRM.Data;
using Q4SRM.RawFileIO;

namespace Q4SRMTests
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
            using (var reader = new XCalDataReader(rawPath))
            {
                var settings = new SettingsData()
                {
                    DefaultIntensityThreshold = 10000
                };

                var results = reader.ReadRawData(settings);
                if (results == null)
                {
                    return;
                }

                CompoundData.WriteCombinedResultsToFile(resultPath, results, settings);
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
