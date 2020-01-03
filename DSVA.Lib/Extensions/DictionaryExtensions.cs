using System.Collections.Generic;
using System.Linq;

namespace DSVA.Lib.Extensions
{
    public static class DictionaryExtensions
    {
        public static IEnumerable<TValue> ToOrderedValues<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) =>
            dictionary.OrderBy(x => x.Key).Select(x => x.Value);

        public static IEnumerable<(TKey, TValue)> AsTupples<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) =>
            dictionary.Select(x => (x.Key, x.Value));

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue fallback = default(TValue)) =>
            dictionary.ContainsKey(key) ? dictionary[key] : fallback;
    }
}
