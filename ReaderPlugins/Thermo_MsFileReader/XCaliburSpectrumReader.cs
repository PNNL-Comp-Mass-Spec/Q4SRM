using System;
using System.Collections.Generic;
using System.Linq;
using Q4SRM.Data;
using Q4SRM.DataReaders;
using ThermoRawFileReader;

namespace Thermo_MsFileReader
{
    class XCaliburSpectrumReader : ISpectraDataReader
    {
        public string DatasetPath { get; }

        public XCaliburSpectrumReader(string datasetPath)
        {
            DatasetPath = datasetPath;
        }

        public List<CompoundData> ReadSpectraData(List<CompoundData> combinedTransitions)
        {
            var scanTimes = new Dictionary<int, double>();
            using (var rawReader = new XRawFileIO(DatasetPath))
            {
                var numScans = rawReader.GetNumScans();
                if (numScans <= 0)
                {
                    // dataset has no MS data. Return.
                    return null;
                }

                for (var i = 1; i <= numScans; i++)
                {
                    var err = rawReader.GetRetentionTime(i, out double time);
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

                rawReader.ScanInfoCacheMaxSize = 2;

                // read spectra data for each transition from the file, in an optimized fashion
                foreach (var scan in scansAndTargets)
                {
                    rawReader.GetScanInfo(scan.Key, out clsScanInfo scanInfo);
                    var preMz = scanInfo.ParentIonMZ;
                    rawReader.GetScanData(scan.Key, out var mzs, out var intensities);

                    foreach (var compound in scan.Value.Where(x => Math.Abs(x.PrecursorMz - preMz) < 0.01))
                    {
                        foreach (var massRange in scanInfo.MRMInfo.MRMMassList)
                        {
                            var match = compound.Transitions.FirstOrDefault(x => massRange.StartMass <= x.ProductMz && x.ProductMz <= massRange.EndMass);
                            if (match == null)
                            {
                                continue;
                            }

                            for (var j = 0; j < mzs.Length; j++)
                            {
                                var mz = mzs[j];
                                var intensity = intensities[j];

                                if (massRange.StartMass <= mz && mz <= massRange.EndMass)
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

        public static bool CheckDependencies()
        {
            // TODO: Do something with the returned string in GUI mode.
            var installed = XRawFileIO.IsMSFileReaderInstalled(out string error);
            if (!installed)
            {
                System.Console.WriteLine("MSFileReader loading error: {0}", error);
            }
            return installed;
        }
    }
}
