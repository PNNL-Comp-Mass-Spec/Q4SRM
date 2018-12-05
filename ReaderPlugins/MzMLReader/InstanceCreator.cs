using System;
using System.Collections.Generic;
using Q4SRM.DataReaders;
using Q4SRM.MethodReaders;

namespace MzMLReader
{
    public class InstanceCreator : IInstanceCreator
    {
        public ISpectraDataReader CreateSpectraReader(string datasetPath)
        {
            return new MzMLSpectrumReader(datasetPath);
        }

        public IMethodReader CreateMethodReader(string datasetPath, string methodFilePath = null)
        {
            if (methodFilePath == null)
            {
                // TODO: Blow up properly.
                return null;
            }

            return new TsvMethodParser(methodFilePath);
        }

        public SystemArchitectures SupportedArchitectures => SystemArchitectures.Both;
        public ReaderPriority ReaderPriority => ReaderPriority.SpecificDatasetType;
        public DatasetTypes SupportedDatasetTypes => DatasetTypes.MzML;
        public CanReadError CanReadDataset(string datasetPath)
        {
            if (datasetPath.EndsWith(".mzml", StringComparison.OrdinalIgnoreCase) ||
                datasetPath.EndsWith(".mzml.gz", StringComparison.OrdinalIgnoreCase))
            {
                return CanReadError.None;
            }

            return CanReadError.UnsupportedFormat;
        }

        public IEnumerable<DatasetTypeFilterSpec> DatasetFilters => new[]
        {
            new DatasetTypeFilterSpec("mzML", "*.mzML", "mzML file", false),
            new DatasetTypeFilterSpec("mzML.gz", "*.mzML.gz", "gzipped mzML file", false)
        };
    }
}
