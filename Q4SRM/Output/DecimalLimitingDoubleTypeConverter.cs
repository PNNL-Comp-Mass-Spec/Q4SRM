using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using PRISM;

namespace Q4SRM.Output
{
    /// <summary>
    /// Implementation of CsvHelper's DoubleConverter that limits the decimal places to 4 digits for values &lt; 10, with fewer decimal places for larger values (by 4 - log10(val))
    /// </summary>
    public class DecimalLimitingDoubleTypeConverter : DoubleConverter
    {
        /// <summary>
        /// Convert to string
        /// </summary>
        /// <param name="value"></param>
        /// <param name="row"></param>
        /// <param name="memberMapData"></param>
        /// <returns></returns>
        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value is double d)
            {
                return StringUtilities.DblToString(d, 4, true);
            }

            return base.ConvertToString(value, row, memberMapData);
        }
    }
}
