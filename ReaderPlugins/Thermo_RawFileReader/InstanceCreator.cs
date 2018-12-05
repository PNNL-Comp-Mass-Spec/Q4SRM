using System;
using System.Collections.Generic;
using System.IO;
using Q4SRM.DataReaders;

namespace Thermo_RawFileReader
{
    public class InstanceCreator : IInstanceCreator
    {
        public ISpectraDataReader CreateSpectraReader(string datasetPath)
        {
            return new XCaliburSpectrumReader(datasetPath);
        }

        public IMethodReader CreateMethodReader(string datasetPath, string methodFilePath = null)
        {
            return new XCaliburMethodReader(datasetPath);
        }

        public SystemArchitectures SupportedArchitectures => SystemArchitectures.x64;
        public ReaderPriority ReaderPriority => ReaderPriority.SpecificDatasetTypeAndArchitecture;
        public DatasetTypes SupportedDatasetTypes => DatasetTypes.ThermoRaw;
        public CanReadError CanReadDataset(string datasetPath)
        {
            if (datasetPath.EndsWith(".raw", StringComparison.OrdinalIgnoreCase) && File.Exists(datasetPath))
            {
                return CanReadError.None;
            }

            return CanReadError.UnsupportedFormat;
        }

        public IEnumerable<DatasetTypeFilterSpec> DatasetFilters => new[]
        {
            new DatasetTypeFilterSpec("RAW", "*.RAW", "Thermo .RAW dataset", false)
        };
    }
}
