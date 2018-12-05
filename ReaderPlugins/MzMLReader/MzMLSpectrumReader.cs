using System;
using System.Collections.Generic;
using System.Linq;
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
            var reader = new SimpleMzMLReader(DatasetPath);
            if (reader.NumSpectra <= 0) // Will read to the SpectrumList
            {
                // ERROR: No spectra - file was created without the flag --srmAsSpectra (--simAsSpectra is for entirely different data)
                // TODO: Implement and use a chromatogram reader
                return null;
            }

            foreach (var spectrum in reader.ReadAllSpectra(true))
            {
                if (!(spectrum is SimpleMzMLReader.SimpleProductSpectrum srmSpec))
                {
                    continue;
                }

                var time = srmSpec.ElutionTime;
                var precursorMz = srmSpec.MonoisotopicMz;
                var matches = combinedTransitions.Where(x => x.StartTimeMinutes <= time && time <= x.StopTimeMinutes && Math.Abs(x.PrecursorMz - precursorMz) < 0.01);

                foreach (var match in matches)
                {
                    foreach (var trans in match.Transitions)
                    {
                        foreach (var scanWindow in srmSpec.ScanWindows.Where(x => x.LowerLimit <= trans.ProductMz && trans.ProductMz <= x.UpperLimit))
                        {
                            //foreach (var peakMatch in srmSpec.Peaks.Where(x => Math.Abs(trans.ProductMz - x.Mz) < 0.01))
                            foreach (var peakMatch in srmSpec.Peaks.Where(x => scanWindow.LowerLimit <= x.Mz && x.Mz <= scanWindow.UpperLimit))
                            {
                                var intensity = peakMatch.Intensity;
                                trans.IntensitySum += intensity;
                                trans.Intensities.Add(new TransitionData.DataPoint(srmSpec.ScanNumber, time, intensity));
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
