using System;
using System.Collections.Generic;
using System.Linq;

namespace Q4SRM.Data
{
    public static class ExtensionMethods
    {
        public static double Median(this IEnumerable<double> data)
        {
            if (data == null)
            {
                return 0;
            }
            var list = data.ToList();
            if (list.Count == 0)
            {
                return 0;
            }

            list.Sort();

            // Integer division
            var mid = list.Count / 2;
            if (list.Count % 2 == 0)
            {
                // even number of items, must average the middle 2
                var int1 = list[mid - 1];
                var int2 = list[mid];
                return (int1 + int2) / 2.0;
            }

            // odd number of items, integer division will give us the center index
            return list[mid];
        }

        public static double Median<T>(this IEnumerable<T> data, Func<T, double> selector)
        {
            return data.Select(selector).Median();
        }
    }
}
