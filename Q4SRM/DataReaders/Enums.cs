using System;

namespace Q4SRM.DataReaders
{
    [Flags]
    public enum SystemArchitectures
    {
        x86 = 0x01,
        x64 = 0x02,
        Both = 0x03,
    }

    [Flags]
    public enum DatasetTypes
    {
        ThermoRaw = 0x01,
        AgilentD  = 0x02,
        MzML = 0x04,
    }

    public enum CanReadError
    {
        None = 0,
        UnsupportedFormat,
        MissingDependency,
    }

    public enum ReaderPriority
    {
        SpecificDatasetTypeAndArchitecture = 1,
        SpecificDatasetType = 2,
        SpecificArchitecture = 3,
        Generic = 10,
    }
}
