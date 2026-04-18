using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace TypedHttp.Model;

internal static class ImmutableByValArray
{
    public static ImmutableByValArray<T> Create<T>(ReadOnlySpan<T> values)
        => new([ ..values ]);
}

[CollectionBuilder(typeof(ImmutableByValArray),
                   nameof(ImmutableByValArray.Create))]
internal readonly struct ImmutableByValArray<T>(ImmutableArray<T> array)
    : IReadOnlyList<T>, IEquatable<ImmutableByValArray<T>>
{
    private readonly ImmutableArray<T> _array = array;

    public ImmutableArray<T> Array => _array;

    public ImmutableArray<T>.Enumerator GetEnumerator()
        => _array.GetEnumerator();

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => ((IEnumerable<T>)_array).GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
        => ((IEnumerable)_array).GetEnumerator();

    /// <inheritdoc cref="ImmutableArray{T}.Length" />
    public int Length => _array.Length;

    /// <inheritdoc />
    public int Count => ((IReadOnlyCollection<T>)_array).Count;

    /// <inheritdoc />
    public T this[int index] => ((IReadOnlyList<T>)_array)[index];

    /// <inheritdoc />
    public bool Equals(ImmutableByValArray<T> other)
        => _array.SequenceEqual(other._array);

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ImmutableByValArray<T> other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var value in _array) { hash.Add(value); }

        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString() => $"[{string.Join(", ", _array)}]";
}

internal static class ImmutableArrayExtensions
{
    public static ImmutableByValArray<T> ByVal<T>(this ImmutableArray<T> array)
        => new(array);
}
