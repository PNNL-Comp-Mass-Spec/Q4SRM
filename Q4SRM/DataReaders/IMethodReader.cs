using System.Collections.Generic;
using Q4SRM.Data;

namespace Q4SRM.DataReaders
{
    public interface IMethodReader
    {
        string DatasetPath { get; }

        /// <summary>
        /// Read all of the transition data from the instrument method data
        /// </summary>
        /// <returns></returns>
        List<TransitionData> GetAllTransitions();
    }
}
