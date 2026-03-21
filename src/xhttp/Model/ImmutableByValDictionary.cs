using System.Collections;
using System.Collections.Immutable;

namespace Xhttp.Model;

internal sealed class ImmutableByValDictionary<TKey, TValue>(ImmutableDictionary<TKey, TValue> dictionary)
    : IReadOnlyDictionary<TKey, TValue>, IEquatable<ImmutableByValDictionary<TKey, TValue>> where TKey : notnull
{
    /// <inheritdoc />
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() { return dictionary.GetEnumerator(); }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() { return ((IEnumerable)dictionary).GetEnumerator(); }

    /// <inheritdoc />
    public int Count => dictionary.Count;

    /// <inheritdoc />
    public bool ContainsKey(TKey key) { return dictionary.ContainsKey(key); }

    /// <inheritdoc />
    public bool TryGetValue(TKey key, out TValue value) { return dictionary.TryGetValue(key, out value); }

    /// <inheritdoc />
    public TValue this[TKey key] => dictionary[key];

    /// <inheritdoc />
    public IEnumerable<TKey> Keys => dictionary.Keys;

    /// <inheritdoc />
    public IEnumerable<TValue> Values => dictionary.Values;

    /// <inheritdoc />
    public bool Equals(ImmutableByValDictionary<TKey, TValue>? other)
    {
        if (other is null) return false;
        foreach (var kv in dictionary)
        {
            if (!other.TryGetValue(kv.Key!, out var value) || !dictionary.ValueComparer.Equals(value, kv.Value))
                return false;
        }
        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is ImmutableByValDictionary<TKey, TValue> other && Equals(other);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"[ {string.Join(", ", dictionary.Select(kv => $"[{kv.Key}] = {kv.Value}"))} ]";
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hc = new HashCode();
        foreach (var kv in dictionary)
        {
            hc.Add(kv.Key!);
            hc.Add(kv.Value);
        }
        return hc.ToHashCode();
    }
}

internal static class ImmutableDictionaryExtensions
{
    public static ImmutableByValDictionary<TKey, TValue> ByVal<TKey, TValue>(
        this ImmutableDictionary<TKey, TValue> array) where TKey : notnull
        => new(array);
}
