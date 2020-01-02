using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DSVA.Lib.Extensions
{
    public static class DictionaryExtensions
    {
        public static IEnumerable<TValue> ToOrderedValues<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) =>
            dictionary.OrderBy(x => x.Key).Select(x => x.Value);
    }
}
