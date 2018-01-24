﻿using System;
using System.Linq;
using NUnit.Framework;
using SrmHeavyQC;

namespace SrmHeavyQCTests
{
    [TestFixture]
    public class MiscTests
    {
        [Test]
        public void TestParseCommentLine()
        {
            //var testComment = @"# DefaultThreshold: 10000";
            var testComment = @"# DefaultThreshold: 10000; CompoundThresholdsFile: ""F:\Temp\RandomFile.tsv""; SHA1Hash 123456789abcdef";
            var settings = new SettingsData();
            settings.PopulateFromTsvComment(testComment);
            Console.WriteLine("DefaultThreshold: {0}", settings.DefaultThreshold);
            Console.WriteLine("CompoundThresholdFilePath: {0}", settings.CompoundThresholdFilePath);
            Console.WriteLine("CompoundThresholdFileSha1Hash: {0}", settings.CompoundThresholdFileSha1Hash);
        }

        [Test]
        public void TestRoundTrip()
        {
            var defaultThreshold = 10000;
            var filePath = ""; // @"F:\Temp\RandomFile.tsv";
            var sha1 = ""; // @"123456789abcdef";
            var settings = new SettingsData()
            {
                DefaultThreshold = 10000,
                CompoundThresholdFilePath = filePath,
                CompoundThresholdFileSha1Hash = sha1,
            };

            var asComment = "# " + settings.ConvertToTsvComment();
            var parsedSettings = new SettingsData();
            parsedSettings.PopulateFromTsvComment(asComment);

            Assert.AreEqual(defaultThreshold, parsedSettings.DefaultThreshold);
            Assert.AreEqual(filePath, parsedSettings.CompoundThresholdFilePath);
            Assert.AreEqual(sha1, parsedSettings.CompoundThresholdFileSha1Hash);
            Assert.True(settings.SettingsEquals(parsedSettings));
        }

        [Test]
        public void TestReadResults()
        {
            var file = @"F:\SRM_data\Rush2_p14RR_62_26Oct17_Smeagol-WRUSHCol3_75x20a_heavyPeaks.tsv";
            var results = SrmCombinedResult.ReadCombinedResultsFile(file).ToList();
            Console.WriteLine(results.Count);
        }
    }
}