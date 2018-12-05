using System.Collections.Generic;
using Q4SRM.Data;

namespace Q4SRM.DataReaders
{
    public interface ISpectraDataReader
    {
        string DatasetPath { get; }
        List<CompoundData> ReadSpectraData(List<CompoundData> combinedTransitions);
    }
}
