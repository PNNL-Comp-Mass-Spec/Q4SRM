using System.Collections.Generic;
using Q4SRM.Data;
using Q4SRM.DataReaders;
using Q4SRM.MethodReaders;
using ThermoFisher.CommonCore.Data.Business;

namespace Thermo_RawFileReader
{
    public class XCaliburMethodReader : IMethodReader
    {
        public string DatasetPath { get; }
        private bool canUseCompoundNames = false;

        public XCaliburMethodReader(string datasetPath)
        {
            DatasetPath = datasetPath;
        }

        /// <summary>
        /// Read all of the transition data from the instrument method data
        /// </summary>
        /// <returns></returns>
        public List<TransitionData> GetAllTransitions()
        {
            var srmTransitions = new List<TransitionData>();
            using (var rawReader = RawFileReaderFactory.ReadFile(DatasetPath))
            {
                //Console.WriteLine($"File \"{RawFilePath}\": {rawReader.InstrumentMethodsCount} instrument methods");
                for (var i = 0; i < rawReader.InstrumentMethodsCount; i++)
                {
                    var method = rawReader.GetInstrumentMethod(i);
                    //Console.WriteLine($"File \"{RawFilePath}\": InstMethod string length: {method.Length}");
                    if (string.IsNullOrWhiteSpace(method))
                    {
                        continue;
                    }
                    var parsed = new XCalInstMethodParser(method);
                    if (parsed.UsesCompoundName)
                    {
                        canUseCompoundNames = true;
                    }
                    srmTransitions.AddRange(parsed.ParseSrmTable());
                }
            }

            //Console.WriteLine($"File \"{RawFilePath}\": {srmTransitions.Count} transitions in instrument method");
            return srmTransitions;
        }
    }
}
