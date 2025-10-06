namespace DemonsGate.Core.Extensions.Dictionaries;

/// <summary>
///     Extension methods for Dictionary types to optimize common operations.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    ///     Gets the list associated with the specified key, or creates a new list if the key does not exist.
    ///     This reduces duplicate lookup code and improves performance by avoiding repeated TryGetValue calls.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the lists.</typeparam>
    /// <param name="dict">The dictionary to operate on.</param>
    /// <param name="key">The key to look up or create.</param>
    /// <returns>The existing or newly created list for the key.</returns>
    public static List<TValue> GetOrCreateList<TKey, TValue>(this Dictionary<TKey, List<TValue>> dict, TKey key)
        where TKey : notnull
    {
        if (dict.TryGetValue(key, out var list))
        {
            return list;
        }

        list = [];
        dict[key] = list;
        return list;
    }
}
