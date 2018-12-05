using System;
using System.Collections.Generic;
using Q4SRM.Data;
using Q4SRM.DataReaders;

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
            throw new NotImplementedException();
        }
    }
}
