using System.Collections.Generic;
using SrmHeavyQC;

namespace SrmHeavyChecker
{
    public interface IOptions : ISettingsData
    {
        string RawFilePath { get; }

        string OutputFolder { get; }

        int MaxThreads { get; }

        IList<string> FilesToProcess { get; }

        bool OverwriteOutput { get; }
    }
}
