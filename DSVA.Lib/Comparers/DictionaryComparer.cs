using DSVA.Lib.Extensions;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DSVA.Lib.Comparers
{
    public class VectorClockComparer : IComparer<IDictionary<int, long>>
    {
        public int Compare([AllowNull] IDictionary<int, long> x, [AllowNull] IDictionary<int, long> y)
        {
            // All x >= y
            if (x.All(z => z.Value >= y.GetOrDefault(z.Key)))
                return 1;
            // All y >= x
            else if (y.All(z => z.Value >= x.GetOrDefault(z.Key)))
                return -1;
            else return 0;
        }
    }
}
