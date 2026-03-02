using System.Collections;
using System.Collections.Immutable;

namespace Xhttp.Model;

internal readonly struct ImmutableByValDictionary<TKey, TValue>(
    ImmutableDictionary<TKey, TValue> dictionary)
    : IReadOnlyDictionary<TKey, TValue>,
      IEquatable<ImmutableByValDictionary<TKey, TValue>>
    where TKey : notnull
{
    public ImmutableDictionary<TKey, TValue> Dictionary => dictionary;

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        => dictionary.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
        => ((IEnumerable)dictionary).GetEnumerator();

    /// <inheritdoc />
    public int Count => dictionary.Count;

    /// <inheritdoc />
    public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);

    /// <inheritdoc />
    public bool TryGetValue(TKey key, out TValue value)
        => dictionary.TryGetValue(key, out value);

    /// <inheritdoc />
    public TValue this[TKey key] => dictionary[key];

    /// <inheritdoc />
    public IEnumerable<TKey> Keys => dictionary.Keys;

    /// <inheritdoc />
    public IEnumerable<TValue> Values => dictionary.Values;

    /// <inheritdoc />
    public bool Equals(ImmutableByValDictionary<TKey, TValue> other)
    {
        foreach (var pair in dictionary)
        {
            if (!other.TryGetValue(pair.Key!, out var otherValue)
             || !EqualityComparer<TValue>.Default.Equals(pair.Value,
                                                         otherValue))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ImmutableByValDictionary<TKey, TValue> other
            && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var pair in dictionary)
        {
            hash.Add(pair.Key!);
            hash.Add(pair.Value);
        }

        return hash.ToHashCode();
    }
}
