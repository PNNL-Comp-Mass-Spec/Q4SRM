using System;
using System.Collections.Generic;
using System.IO;
using Q4SRM.DataReaders;

namespace Thermo_MsFileReader
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

        public SystemArchitectures SupportedArchitectures => SystemArchitectures.Both;
        public ReaderPriority ReaderPriority => ReaderPriority.SpecificDatasetType;
        public DatasetTypes SupportedDatasetTypes => DatasetTypes.ThermoRaw;
        public CanReadError CanReadDataset(string datasetPath)
        {
            if (!XCaliburSpectrumReader.CheckDependencies())
            {
                return CanReadError.MissingDependency;
            }

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
