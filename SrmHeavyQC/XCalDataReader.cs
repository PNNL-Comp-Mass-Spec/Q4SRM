using System;
using System.Collections.Generic;
using System.Linq;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoRawFileReader;

namespace SrmHeavyQC
{
    public class XCalDataReader : IDisposable
    {
        public string RawFilePath { get; }
        private XRawFileIO pnnlRawReaderAdapter = null;
        //private IRawDataPlus rawReader = null;
        private IRawFileThreadManager rawReaderThreader = null;
        private bool useThermoRawFileReader = false;

        public XCalDataReader(string rawFilePath, bool usePNNLReaderAdapter = false)
        {
            RawFilePath = rawFilePath;

            useThermoRawFileReader = usePNNLReaderAdapter;
            if (useThermoRawFileReader)
            {
                pnnlRawReaderAdapter = new XRawFileIO();
                pnnlRawReaderAdapter.LoadMSMethodInfo = true;
                pnnlRawReaderAdapter.ScanInfoCacheMaxSize = 1;
                pnnlRawReaderAdapter.OpenRawFile(RawFilePath);
            }
            else
            {
                //rawReader = RawFileReaderFactory.ReadFile(rawFilePath);
                rawReaderThreader = RawFileReaderFactory.CreateThreadManager(rawFilePath);
            }
        }

        public List<SrmResult> ReadRawData(double tolerancePpm = 20)
        {
            var srmTransitions = GetHeavyTransitions().Select(x => new SrmResult(x, tolerancePpm)).ToList();
            //Console.WriteLine($"File \"{RawFilePath}\": {srmTransitions.Count} heavy transitions in instrument method.");
            if (srmTransitions.Count == 0)
            {
                Console.WriteLine($"ERROR: Could not read instrument methods or find heavy transitions for file \"{RawFilePath}\"");
                return null;
            }
            //var temp = new List<SrmResult>();
            //temp.Add(srmTransitions[0]);
            //srmTransitions = temp;
            // TODO: Could determine heavy peptides by checking names vs. precursor m/z?
            var scanTimes = new Dictionary<int, double>();
            if (useThermoRawFileReader)
            {
                var numScans = pnnlRawReaderAdapter.GetNumScans();
                for (var i = 1; i <= numScans; i++)
                {
                    pnnlRawReaderAdapter.GetRetentionTime(i, out var time);
                    scanTimes.Add(i, time);
                }

                // Map transition start/stop times to scans
                var scansAndTargets = new Dictionary<int, List<SrmResult>>();
                foreach (var scan in scanTimes)
                {
                    var matches = srmTransitions.Where(x => x.Transition.StartTimeMinutes <= scan.Value && scan.Value <= x.Transition.StopTimeMinutes)
                        .ToList();
                    if (matches.Count > 0)
                    {
                        scansAndTargets.Add(scan.Key, matches);
                    }
                }

                // read spectra data for each transition from the file, in an optimized fashion
                foreach (var scan in scansAndTargets)
                {
                    //pnnlRawReaderAdapter.GetScanData2D(scan.Key, out var massIntensity, 0, true);
                    pnnlRawReaderAdapter.GetScanData2D(scan.Key, out var massIntensity, 0, false);
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
            }
            else
            {
                using (var rawReader = rawReaderThreader.CreateThreadAccessor())
                {
                    if (!rawReader.SelectMsData())
                    {
                        // dataset has no MS data. Return.
                        return null;
                    }

                    var header = rawReader.RunHeaderEx;
                    var minScan = header.FirstSpectrum;
                    var maxScan = header.LastSpectrum;
                    var numScans = header.SpectraCount;
                    for (var i = 1; i <= numScans; i++)
                    {
                        var time = rawReader.RetentionTimeFromScanNumber(i);
                        scanTimes.Add(i, time);
                        // Filter string parsing for speed: "+ c NSI SRM ms2 [precursor/parent m/z] [[list of product m/z ranges]]
                    }

                    // Map transition start/stop times to scans
                    var scansAndTargets = new Dictionary<int, List<SrmResult>>();
                    foreach (var scan in scanTimes)
                    {
                        var matches = srmTransitions
                            .Where(x => x.Transition.StartTimeMinutes <= scan.Value && scan.Value <= x.Transition.StopTimeMinutes)
                            .ToList();
                        if (matches.Count > 0)
                        {
                            scansAndTargets.Add(scan.Key, matches);
                        }
                    }

                    // read spectra data for each transition from the file, in an optimized fashion
                    foreach (var scan in scansAndTargets)
                    {
                        //var scanInfo = rawReader.GetScanFiltersFromCompoundName(xxx);
                        var scanInfo = rawReader.GetFilterForScanNumber(scan.Key);
                        //var massRangeCount = scanInfo.MassRangeCount;
                        //var massRange1 = scanInfo.GetMassRange(0);
                        var data = rawReader.GetSimplifiedScan(scan.Key);
                        for (var i = 0; i < data.Masses.Length; i++)
                        {
                            var mz = data.Masses[i];
                            var intensity = data.Intensities[i];
                            foreach (var target in scan.Value.Where(x => x.MinMz <= mz && mz <= x.MaxMz))
                            {
                                target.Area += intensity;
                            }
                        }
                    }
                }
            }

            return srmTransitions;
        }

        public List<SrmCombinedResult> AggregateResults(IEnumerable<SrmResult> results, double threshold, Dictionary<string, CompoundThresholdData> compoundThresholds = null)
        {
            if (compoundThresholds == null)
            {
                compoundThresholds = new Dictionary<string, CompoundThresholdData>();
            }
            var combinedMap = new Dictionary<string, SrmCombinedResult>();
            foreach (var result in results)
            {
                SrmCombinedResult group;
                if (!combinedMap.TryGetValue(result.Transition.CompoundName, out group))
                {
                    group = new SrmCombinedResult(result.Transition) {Threshold = threshold};
                    // Look for and handle custom thresholds
                    if (compoundThresholds.TryGetValue(result.Transition.CompoundName, out var cThreshold))
                    {
                        group.Threshold = cThreshold.Threshold;
                    }
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
            if (useThermoRawFileReader)
            {
                //Console.WriteLine($"File \"{RawFilePath}\": {pnnlRawReaderAdapter.FileInfo.InstMethods.Count} instrument methods");
                foreach (var method in pnnlRawReaderAdapter.FileInfo.InstMethods)
                {
                    //Console.WriteLine($"File \"{RawFilePath}\": InstMethod string length: {method.Length}");
                    var parsed = new XCalInstMethod(method);
                    srmTransitions.AddRange(parsed.ParseSrmTable());
                }
            }
            else
            {
                using (var rawReader = rawReaderThreader.CreateThreadAccessor())
                {
                    //Console.WriteLine($"File \"{RawFilePath}\": {rawReader.InstrumentMethodsCount} instrument methods");
                    for (var i = 0; i < rawReader.InstrumentMethodsCount; i++)
                    {
                        var method = rawReader.GetInstrumentMethod(i);
                        //Console.WriteLine($"File \"{RawFilePath}\": InstMethod string length: {method.Length}");
                        if (string.IsNullOrWhiteSpace(method))
                        {
                            continue;
                        }
                        var parsed = new XCalInstMethod(method);
                        srmTransitions.AddRange(parsed.ParseSrmTable());
                    }
                }
            }

            //Console.WriteLine($"File \"{RawFilePath}\": {srmTransitions.Count} transitions in intrument method");
            return srmTransitions;
        }

        private void CloseReader()
        {
            if (pnnlRawReaderAdapter != null)
            {
                pnnlRawReaderAdapter.Dispose();
                pnnlRawReaderAdapter = null;
            }
            //if (rawReader != null)
            //{
            //    rawReader.Dispose();
            //    rawReader = null;
            //}
            if (rawReaderThreader != null)
            {
                rawReaderThreader.Dispose();
                rawReaderThreader = null;
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
