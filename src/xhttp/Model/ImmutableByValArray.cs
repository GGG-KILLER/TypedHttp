using System.Collections;
using System.Collections.Immutable;

namespace Xhttp.Model;

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
}
