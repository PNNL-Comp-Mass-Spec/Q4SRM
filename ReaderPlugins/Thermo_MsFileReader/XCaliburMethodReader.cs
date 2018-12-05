using System.Collections.Generic;
using Q4SRM.Data;
using Q4SRM.DataReaders;
using Q4SRM.MethodReaders;
using ThermoRawFileReader;

namespace Thermo_MsFileReader
{
    class XCaliburMethodReader : IMethodReader
    {
        public string DatasetPath { get; }
        private bool canUseCompoundNames = false;

        public XCaliburMethodReader(string datasetPath)
        {
            DatasetPath = datasetPath;
        }

        public List<TransitionData> GetAllTransitions()
        {
            var srmTransitions = new List<TransitionData>();
            using (var reader = new XRawFileIO(DatasetPath))
            {
                //Console.WriteLine($"File \"{DatasetPath}\": {reader.FileInfo.InstMethods.Count} instrument methods");
                foreach (var method in reader.FileInfo.InstMethods)
                {
                    //Console.WriteLine($"File \"{DatasetPath}\": InstMethod string length: {method.Length}");
                    var parsed = new XCalInstMethodParser(method);
                    if (parsed.UsesCompoundName)
                    {
                        canUseCompoundNames = true;
                    }
                    srmTransitions.AddRange(parsed.ParseSrmTable());
                }
            }

            //Console.WriteLine($"File \"{DatasetPath}\": {srmTransitions.Count} transitions in instrument method");
            return srmTransitions;
        }
    }
}
