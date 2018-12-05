using System;
using System.Linq;
using NUnit.Framework;
using Q4SRM.Data;
using Thermo_RawFileReader;

namespace Q4SRMTests
{
    [TestFixture]
    public class ReadingTests
    {
        [Test]
        public void TestMethodVantage()
        {
            GetMethod(@"E:\SRM_data\Rush2_p14RR_62_26Oct17_Smeagol-WRUSHCol3_75x20a.raw");
        }

        [Test]
        public void TestMethodAltis()
        {
            GetMethod(@"E:\SRM_data\TdyQc1_Fnl_All_25Oct17_Balzac-W1sta1.raw");
        }

        [Test]
        public void TestProcessVantage()
        {
            ProcessTest(@"E:\SRM_data\Rush2_p14RR_62_26Oct17_Smeagol-WRUSHCol3_75x20a.raw", @"E:\SRM_data\Rush2_p14RR_62_26Oct17_Smeagol-WRUSHCol3_75x20a.tsv");
        }

        [Test]
        public void TestProcessAltis()
        {
            ProcessTest(@"E:\SRM_data\TdyQc1_Fnl_All_25Oct17_Balzac-W1sta1.raw", @"E:\SRM_data\TdyQc1_Fnl_All_25Oct17_Balzac-W1sta1.tsv");
        }

        private void ProcessTest(string rawPath, string resultPath)
        {
            var reader = new XCaliburSpectrumReader(rawPath);
            var settings = new SettingsData()
            {
                DefaultIntensityThreshold = 10000
            };

            var methodReader = new XCaliburMethodReader(rawPath);
            var compounds = methodReader.ReadMethodData(settings);
            var results = reader.ReadSpectraData(compounds);
            if (results == null)
            {
                return;
            }

            CompoundData.WriteCombinedResultsToFile(resultPath, results, settings);
        }

        private void GetMethod(string rawPath)
        {
            var xcal = new XCaliburMethodReader(rawPath);
            var data = xcal.GetAllTransitions();
            foreach (var d in data.OrderBy(x => x.CompoundName))
            {
                Console.WriteLine(d);
            }
        }
    }
}
