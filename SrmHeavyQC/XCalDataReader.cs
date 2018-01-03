using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThermoRawFileReader;

namespace SrmHeavyQC
{
    public class XCalDataReader : IDisposable
    {
        public string RawFilePath { get; }
        private XRawFileIO rawReader = null;

        public XCalDataReader(string rawFilePath)
        {
            RawFilePath = rawFilePath;

            rawReader = new XRawFileIO();
            rawReader.LoadMSMethodInfo = true;
            rawReader.ScanInfoCacheMaxSize = 1;
            rawReader.OpenRawFile(RawFilePath);
        }

        public List<SrmResult> ReadRawData()
        {
            Console.WriteLine("Reading data from file...");
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var srmTransitions = GetHeavyTransitions().Select(x => new SrmResult(x)).ToList();
            var elapsed = sw.Elapsed;
            Console.WriteLine($"ReadTransitions: {elapsed}");
            sw.Restart();
            //var temp = new List<SrmResult>();
            //temp.Add(srmTransitions[0]);
            //srmTransitions = temp;
            // TODO: Could determine heavy peptides by checking names vs. precursor m/z?
            var scanTimes = new Dictionary<int, double>();
            var numScans = rawReader.GetNumScans();
            for (var i = 1; i <= numScans; i++)
            {
                rawReader.GetRetentionTime(i, out var time);
                scanTimes.Add(i, time);
            }
            elapsed = sw.Elapsed;
            Console.WriteLine($"ReadRetentionTimes: {elapsed}");
            sw.Restart();

            // Map transition start/stop times to scans
            var scansAndTargets = new Dictionary<int, List<SrmResult>>();
            foreach (var scan in scanTimes)
            {
                var matches = srmTransitions.Where(x => x.Transition.StartTimeMinutes <= scan.Value && scan.Value <= x.Transition.StopTimeMinutes).ToList();
                if (matches.Count > 0)
                {
                    scansAndTargets.Add(scan.Key, matches);
                }
            }
            elapsed = sw.Elapsed;
            Console.WriteLine($"MapTransitionsToScans: {elapsed}");
            sw.Restart();

            // read spectra data for each transition from the file, in an optimized fashion
            foreach (var scan in scansAndTargets)
            {
                //rawReader.GetScanData2D(scan.Key, out var massIntensity, 0, true);
                rawReader.GetScanData2D(scan.Key, out var massIntensity, 0, false);
                //var peaks = new List<Peak>();
                //for (var i = 0; i < massIntensity.GetLength(1); i++)
                //{
                //    peaks.Add(new Peak(massIntensity[0,i], massIntensity[1,i]));
                //}
                //
                //foreach (var target in scan.Value)
                //{
                //    var result = new SpectrumSection(scan.Key);
                //    result.SpectrumPeaks.AddRange(peaks.Where(x => target.MinMz <= x.Mz && x.Mz <= target.MaxMz));
                //    if (result.SpectrumPeaks.Count > 0)
                //    {
                //        target.SpectraResults.Add(result);
                //    }
                //}

                for (var i = 0; i < massIntensity.GetLength(1); i++)
                {
                    var mz = massIntensity[0, i];
                    var intensity = massIntensity[1, i];
                    foreach (var target in scan.Value.Where(x => x.MinMz <= mz && mz <= x.MaxMz))
                    {
                        target.Area += intensity;
                    }
                }
            }
            elapsed = sw.Elapsed;
            Console.WriteLine($"ReadData: {elapsed}");
            sw.Stop();

            return srmTransitions;
        }

        public List<SrmCombinedResult> AggregateResults(IEnumerable<SrmResult> results, double threshold)
        {
            var combinedMap = new Dictionary<string, SrmCombinedResult>();
            foreach (var result in results)
            {
                SrmCombinedResult group;
                if (!combinedMap.TryGetValue(result.Transition.CompoundName, out group))
                {
                    group = new SrmCombinedResult(result.Transition) {Threshold = threshold};
                    combinedMap.Add(result.Transition.CompoundName, group);
                }

                group.AddTransition(result);
            }

            return combinedMap.Values.ToList();
        }

        /// <summary>
        /// Get the heavy transitions from the transition data
        /// </summary>
        /// <returns></returns>
        public List<SrmTableData> GetHeavyTransitions()
        {
            // TODO: Check for heavy transitions using precursor mass difference 0.5 <= m/z <= 6.0
            return GetAllTransitions().Where(x => x.IsHeavy).ToList();
        }

        /// <summary>
        /// Read all of the transition data from the instrument method data
        /// </summary>
        /// <returns></returns>
        public List<SrmTableData> GetAllTransitions()
        {
            var srmTransitions = new List<SrmTableData>();
            foreach (var method in rawReader.FileInfo.InstMethods)
            {
                var parsed = new XCalInstMethod(method);
                srmTransitions.AddRange(parsed.ParseSrmTable());
            }

            return srmTransitions;
        }

        private void CloseReader()
        {
            if (rawReader != null)
            {
                rawReader.Dispose();
                rawReader = null;
            }
        }

        /// <summary>
        /// Clean up
        /// </summary>
        public void Dispose()
        {
            CloseReader();
            GC.SuppressFinalize(this);
        }

        ~XCalDataReader()
        {
            CloseReader();
        }
    }
}
