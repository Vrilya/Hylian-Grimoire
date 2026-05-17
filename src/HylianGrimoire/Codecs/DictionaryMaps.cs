namespace HylianGrimoire.Codecs;

internal static class DictionaryMaps
{
    public static IReadOnlyDictionary<TValue, TKey> Reverse<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue> source,
        IEqualityComparer<TValue>? comparer = null)
        where TValue : notnull
    {
        var result = comparer is null ? new Dictionary<TValue, TKey>() : new Dictionary<TValue, TKey>(comparer);
        foreach (var pair in source)
        {
            result[pair.Value] = pair.Key;
        }

        return result;
    }
}
