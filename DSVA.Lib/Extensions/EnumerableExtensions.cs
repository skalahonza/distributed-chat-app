using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DSVA.Lib.Extensions
{

    public static class EnumerableExtensions
    {
        private static IEnumerable<KeyValuePair<int, TValue>> EnumAsPairs<TValue>(this IEnumerable<TValue> values)
        {
            int idx = 0;
            foreach (var x in values)
                yield return new KeyValuePair<int, TValue>(idx++, x);
        }

        public static Dictionary<int, TValue> ToDictionary<TValue>(this IEnumerable<TValue> values) =>
            new Dictionary<int, TValue>(values.EnumAsPairs());
    }
}
