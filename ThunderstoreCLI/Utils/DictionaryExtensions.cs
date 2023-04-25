namespace ThunderstoreCLI.Utils;

public static class DictionaryExtensions
{
    public static TValue? GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) where TKey : notnull
    {
        return dict.TryGetValue(key, out var value) ? value : default;
    }
}
