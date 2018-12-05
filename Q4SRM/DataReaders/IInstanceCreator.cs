using System.Collections.Generic;

namespace Q4SRM.DataReaders
{
    public interface IInstanceCreator
    {
        ISpectraDataReader CreateSpectraReader(string datasetPath);

        IMethodReader CreateMethodReader(string datasetPath, string methodFilePath = null);

        /// <summary>
        /// Processor architectures supported by this reader
        /// </summary>
        SystemArchitectures SupportedArchitectures { get; }

        /// <summary>
        /// Preference level for the reader; readers that are for specific dataset types and processor architectures are preferred over more generic readers.
        /// </summary>
        ReaderPriority ReaderPriority { get; }

        /// <summary>
        /// Dataset types supported by this reader
        /// </summary>
        DatasetTypes SupportedDatasetTypes { get; }

        /// <summary>
        /// Returns no error if the dataset can be read by this reader; returns error enum if the format is unsupported or dependencies are missing.
        /// </summary>
        /// <param name="datasetPath"></param>
        /// <returns></returns>
        CanReadError CanReadDataset(string datasetPath);

        IEnumerable<DatasetTypeFilterSpec> DatasetFilters { get; }
    }
}
