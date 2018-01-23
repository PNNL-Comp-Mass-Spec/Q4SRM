using System.Collections.Generic;

namespace SrmHeavyChecker
{
    public interface IOptions
    {
        string RawFilePath { get; set; }

        double DefaultThreshold { get; set; }

        string CompoundThresholdFilePath { get; set; }

        string OutputFolder { get; set; }

        int MaxThreads { get; set; }

        IList<string> FilesToProcess { get; }
    }
}
