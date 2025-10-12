namespace SquidCraft.Core.Extensions.Collections;

/// <summary>
///     Extension methods for collection types to simplify common operations.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    ///     Returns a random element from the collection.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="source">The collection to select from.</param>
    /// <returns>A randomly selected element from the collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the collection is empty.</exception>
    public static T RandomElement<T>(this IList<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.Count == 0)
        {
            throw new InvalidOperationException("Cannot select a random element from an empty collection.");
        }

        return source[Random.Shared.Next(source.Count)];
    }

    /// <summary>
    ///     Returns a random element from the collection, or a default value if the collection is empty.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="source">The collection to select from.</param>
    /// <param name="defaultValue">The default value to return if the collection is empty.</param>
    /// <returns>A randomly selected element from the collection, or the default value if empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
    public static T? RandomElementOrDefault<T>(this IList<T> source, T? defaultValue = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.Count == 0)
        {
            return defaultValue;
        }

        return source[Random.Shared.Next(source.Count)];
    }

    /// <summary>
    ///     Returns multiple random elements from the collection without replacement.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="source">The collection to select from.</param>
    /// <param name="count">The number of elements to select.</param>
    /// <returns>A list of randomly selected elements.</returns>
    /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when count is negative or greater than the collection size.</exception>
    public static List<T> RandomElements<T>(this IList<T> source, int count)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (count < 0 || count > source.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(count), $"Count must be between 0 and {source.Count}.");
        }

        if (count == 0)
        {
            return new List<T>();
        }

        if (count == source.Count)
        {
            return new List<T>(source);
        }

        // Use HashSet to track selected indices for efficient lookup
        var selectedIndices = new HashSet<int>();
        var result = new List<T>(count);

        while (result.Count < count)
        {
            var index = Random.Shared.Next(source.Count);
            if (selectedIndices.Add(index))
            {
                result.Add(source[index]);
            }
        }

        return result;
    }
}
