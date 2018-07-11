using System;
using System.Collections.Generic;
using System.Linq;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;

namespace SrmHeavyQC
{
    public class XCalDataReader : IDisposable
    {
        public string RawFilePath { get; }
        //private IRawDataPlus rawReader = null;
        private IRawFileThreadManager rawReaderThreader = null;
        private bool canUseCompoundNames = false;

        public XCalDataReader(string rawFilePath)
        {
            RawFilePath = rawFilePath;

            //rawReader = RawFileReaderFactory.ReadFile(rawFilePath);
            rawReaderThreader = RawFileReaderFactory.CreateThreadManager(rawFilePath);
        }

        public List<CompoundData> ReadRawData(ISettingsData settings)
        {
            var srmTransitions = GetHeavyTransitions().ToList();
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
            var combSrmTransitions = JoinTransitions(srmTransitions, settings);
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
                var scansAndTargets = new Dictionary<int, List<CompoundData>>();
                foreach (var scan in scanTimes)
                {
                    var matches = combSrmTransitions.Values
                        .Where(x => x.StartTimeMinutes <= scan.Value && scan.Value <= x.StopTimeMinutes).ToList();
                    if (matches.Count > 0)
                    {
                        scansAndTargets.Add(scan.Key, matches);
                    }
                }

                // This works for the Altis file, but not for the Vantage file
                // Should be able to key on 'canUseCompoundNames'
                //var compounds = rawReader.GetCompoundNames();

                // read spectra data for each transition from the file, in an optimized fashion
                foreach (var scan in scansAndTargets)
                {
                    // This works for the Altis file, but not for the Vantage file
                    //var scanInfoX = rawReader.GetScanFiltersFromCompoundName(scan.Value[0].Transition.CompoundName);
                    var scanInfo = rawReader.GetFilterForScanNumber(scan.Key);
                    var reaction = scanInfo.GetReaction(0);
                    var preMz = reaction.PrecursorMass;
                    var data = rawReader.GetSimplifiedScan(scan.Key);

                    foreach (var compound in scan.Value.Where(x => Math.Abs(x.PrecursorMz - preMz) < 0.01))
                    {
                        for (var i = 0; i < scanInfo.MassRangeCount; i++)
                        {
                            var massRange = scanInfo.GetMassRange(i);
                            var match = compound.Transitions.FirstOrDefault(x => massRange.Low <= x.ProductMz && x.ProductMz <= massRange.High);
                            if (match == null)
                            {
                                continue;
                            }

                            for (var j = 0; j < data.Masses.Length; j++)
                            {
                                var mz = data.Masses[j];
                                var intensity = data.Intensities[j];

                                if (massRange.Low <= mz && mz <= massRange.High)
                                {
                                    match.IntensitySum += intensity;
                                    match.Intensities.Add(new TransitionData.DataPoint(scan.Key, scanTimes[scan.Key], intensity));
                                    if (intensity > match.MaxIntensity)
                                    {
                                        match.MaxIntensity = intensity;
                                        match.MaxIntensityTime = scanTimes[scan.Key];
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return combSrmTransitions.Values.ToList();
        }

        private Dictionary<string, CompoundData> JoinTransitions(List<TransitionData> data, ISettingsData settings)
        {
            var compoundThresholds = settings.LoadCompoundThresholds();
            if (compoundThresholds == null)
            {
                compoundThresholds = new Dictionary<string, CompoundThresholdData>();
            }
            var combined = new Dictionary<string, CompoundData>();
            foreach (var item in data)
            {
                if (!combined.TryGetValue(item.CompoundName, out var group))
                {
                    group = new CompoundData(item) { Threshold = settings.DefaultThreshold, EdgeNETThresholdMinutes = settings.EdgeNETThresholdMinutes };
                    // Look for and handle custom thresholds
                    if (compoundThresholds.TryGetValue(item.CompoundName, out var cThreshold))
                    {
                        group.Threshold = cThreshold.Threshold;
                    }
                    combined.Add(group.CompoundName, group);
                }
                group.Transitions.Add(item);
            }

            return combined;
        }

        /// <summary>
        /// Get the heavy transitions from the transition data
        /// </summary>
        /// <returns></returns>
        public List<TransitionData> GetHeavyTransitions()
        {
            // TODO: Check for heavy transitions using precursor mass difference 0.5 <= m/z <= 6.0
            return GetAllTransitions().Where(x => x.IsHeavy).ToList();
        }

        /// <summary>
        /// Read all of the transition data from the instrument method data
        /// </summary>
        /// <returns></returns>
        public List<TransitionData> GetAllTransitions()
        {
            var srmTransitions = new List<TransitionData>();
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
                    if (parsed.UsesCompoundName)
                    {
                        canUseCompoundNames = true;
                    }
                    srmTransitions.AddRange(parsed.ParseSrmTable());
                }
            }

            //Console.WriteLine($"File \"{RawFilePath}\": {srmTransitions.Count} transitions in intrument method");
            return srmTransitions;
        }

        private void CloseReader()
        {
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
