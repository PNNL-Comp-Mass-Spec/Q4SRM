using System;
using System.Collections.Generic;
using System.Linq;
using PSI_Interface.CV;
using PSI_Interface.MSData;
using Q4SRM.Data;
using Q4SRM.DataReaders;

namespace MzMLReader
{
    public class MzMLSpectrumReader: ISpectraDataReader
    {
        public string DatasetPath { get; }

        public MzMLSpectrumReader(string datasetPath)
        {
            DatasetPath = datasetPath;
        }

        public List<CompoundData> ReadSpectraData(List<CompoundData> combinedTransitions)
        {
            using (var reader = new SimpleMzMLReader(DatasetPath))
            {

                foreach (var spectrum in reader.ReadAllSpectra(true))
                {
                    if (spectrum.Precursors.Count == 0)
                    {
                        continue;
                    }

                    var time = spectrum.ScanStartTime;
                    var precursorMz = spectrum.Precursors[0].SelectedIons[0].SelectedIonMz;
                    var matches = combinedTransitions.Where(x => x.StartTimeMinutes <= time && time <= x.StopTimeMinutes && Math.Abs(x.PrecursorMz - precursorMz) < 0.01);

                    foreach (var match in matches)
                    {
                        foreach (var trans in match.Transitions)
                        {
                            foreach (var scanWindow in spectrum.ScanWindows.Where(x => x.LowerLimit <= trans.ProductMz && trans.ProductMz <= x.UpperLimit))
                            {
                                //foreach (var peakMatch in srmSpec.Peaks.Where(x => Math.Abs(trans.ProductMz - x.Mz) < 0.01))
                                foreach (var peakMatch in spectrum.Peaks.Where(x => scanWindow.LowerLimit <= x.Mz && x.Mz <= scanWindow.UpperLimit))
                                {
                                    var intensity = peakMatch.Intensity;
                                    trans.IntensitySum += intensity;
                                    trans.Intensities.Add(new TransitionData.DataPoint(spectrum.ScanNumber, time, intensity));
                                    if (intensity > trans.MaxIntensity)
                                    {
                                        trans.MaxIntensity = intensity;
                                        trans.MaxIntensityTime = time;
                                    }
                                }
                            }
                        }
                    }
                }

                var medianScanTimeDiff = 0.001;
                foreach (var chromatogram in reader.ReadAllChromatograms(true))
                {
                    if (chromatogram.CVParams.Any(x => x.TermInfo.Cvid == CV.CVID.MS_total_ion_current_chromatogram))
                    {
                        // First chromatogram, use it to determine approximate scan times
                        var diffs = new List<double>(chromatogram.Times.Length);
                        var lastTime = 0.0;
                        foreach (var time in chromatogram.Times)
                        {
                            diffs.Add(time - lastTime);
                            lastTime = time;
                        }

                        diffs.Sort();
                        medianScanTimeDiff = diffs[diffs.Count / 2];
                    }

                    if (!chromatogram.CVParams.Any(x => x.TermInfo.Cvid == CV.CVID.MS_selected_reaction_monitoring_chromatogram))
                    {
                        continue;
                    }

                    var precursorMz = chromatogram.Precursor.IsolationWindow.TargetMz;
                    var productMz = chromatogram.Product.TargetMz;
                    var matches = combinedTransitions.Where(x => Math.Abs(x.PrecursorMz - precursorMz) < 0.01);

                    foreach (var match in matches)
                    {
                        foreach (var trans in match.Transitions.Where(x => Math.Abs(x.ProductMz - productMz) < 0.01))
                        {
                            for (var i = 0; i < chromatogram.Times.Length; i++)
                            {
                                var time = chromatogram.Times[i];
                                var intensity = chromatogram.Intensities[i];
                                trans.IntensitySum += intensity;
                                var estimatedScanNum = (int) Math.Round(time / medianScanTimeDiff, MidpointRounding.AwayFromZero);
                                trans.Intensities.Add(new TransitionData.DataPoint(estimatedScanNum, time, intensity));
                                if (intensity > trans.MaxIntensity)
                                {
                                    trans.MaxIntensity = intensity;
                                    trans.MaxIntensityTime = time;
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
