using System;
using System.Collections.Generic;
using System.Linq;
using Q4SRM.Data;
using Q4SRM.DataReaders;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;

namespace Thermo_RawFileReader
{
    public class XCaliburSpectrumReader : ISpectraDataReader
    {
        public string DatasetPath { get; }
        //private IRawDataPlus rawReader = null;
        //private IRawFileThreadManager rawReaderThreader = null;
        private bool canUseCompoundNames = false;

        public XCaliburSpectrumReader(string rawFilePath)
        {
            DatasetPath = rawFilePath;

            //rawReader = RawFileReaderFactory.ReadFile(rawFilePath);
            //rawReaderThreader = RawFileReaderFactory.CreateThreadManager(rawFilePath);
        }

        public List<CompoundData> ReadSpectraData(List<CompoundData> combinedTransitions)
        {
            var scanTimes = new Dictionary<int, double>();
            //using (var rawReader = rawReaderThreader.CreateThreadAccessor())
            using (var rawReader = RawFileReaderFactory.ReadFile(DatasetPath))
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
                    var matches = combinedTransitions
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

            return combinedTransitions.ToList();
        }
    }
}
