using System;
using System.Collections.Generic;
using System.IO;
using Q4SRM.DataReaders;

namespace AgilentReader
{
    public class InstanceCreator // : IInstanceCreator // TODO: Uncomment when implemented
    {
        public ISpectraDataReader CreateSpectraReader(string datasetPath)
        {
            throw new NotImplementedException();
        }

        public IMethodReader CreateMethodReader(string datasetPath, string methodFilePath = null)
        {
            throw new NotImplementedException();
        }

        public SystemArchitectures SupportedArchitectures => SystemArchitectures.x64;
        public ReaderPriority ReaderPriority => ReaderPriority.SpecificDatasetTypeAndArchitecture;
        public DatasetTypes SupportedDatasetTypes => DatasetTypes.AgilentD;

        public CanReadError CanReadDataset(string datasetPath)
        {
            if (datasetPath.EndsWith(".D", StringComparison.OrdinalIgnoreCase) && Directory.Exists(datasetPath))
            {
                // TODO: Check for some Agilent-specific file.
                return CanReadError.None;
            }

            // TODO: Check the name ending, that it is a folder, and that it has some Agilent-specific file.
            throw new NotImplementedException();
        }

        public IEnumerable<DatasetTypeFilterSpec> DatasetFilters => new[]
        {
            new DatasetTypeFilterSpec("D", "*.D", "Agilent dataset folder", true)
        };
    }
}
