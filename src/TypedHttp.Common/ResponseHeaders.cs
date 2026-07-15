using System.Collections;
using System.Net.Http.Headers;
using JetBrains.Annotations;

namespace TypedHttp;

/// <summary>
/// An immutable, case-insensitive snapshot of an HTTP response's headers, including the
/// content headers (e.g. <c>Content-Type</c>, <c>Content-Length</c>).
/// Unlike <see cref="HttpResponseHeaders"/>, it holds no reference to the response message.
/// </summary>
[PublicAPI]
public sealed class ResponseHeaders : IReadOnlyCollection<KeyValuePair<string, IReadOnlyList<string>>>
{
    private static readonly string[] s_emptyValues = [ ];

    /// <summary>
    /// A snapshot with no headers.
    /// </summary>
    public static ResponseHeaders Empty { get; } =
        new(new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase));

    private readonly Dictionary<string, IReadOnlyList<string>> _headers;

    private ResponseHeaders(Dictionary<string, IReadOnlyList<string>> headers)
    {
        _headers = headers;
    }

    /// <summary>
    /// Creates a snapshot of the provided message's response and content headers.
    /// </summary>
    /// <param name="message">The message whose headers will be copied.</param>
    /// <returns>The created snapshot.</returns>
    public static ResponseHeaders FromMessage(HttpResponseMessage message)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));

        var headers = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        copyHeaders(headers, message.Headers!);
        if (message.Content is not null) copyHeaders(headers, message.Content.Headers!);
        return headers.Count == 0 ? Empty : new ResponseHeaders(headers);

        static void copyHeaders(Dictionary<string, IReadOnlyList<string>> target, HttpHeaders source)
        {
            foreach (var header in source)
            {
                target[header.Key] = target.TryGetValue(header.Key, out var existing)
                                         ? [ .. existing!, .. header.Value! ]
                                         : [ .. header.Value! ];
            }
        }
    }

    /// <summary>
    /// The number of distinct header names in this snapshot.
    /// </summary>
    public int Count => _headers.Count;

    /// <summary>
    /// Checks whether a header with the provided name exists.
    /// </summary>
    /// <param name="name">The (case-insensitive) header name.</param>
    /// <returns>Whether the header exists.</returns>
    public bool Contains(string name) => _headers.ContainsKey(name);

    /// <summary>
    /// Returns all values for the provided header name, or an empty list if the header does not exist.
    /// </summary>
    /// <param name="name">The (case-insensitive) header name.</param>
    /// <returns>The header's values, or an empty list.</returns>
    public IReadOnlyList<string> GetValues(string name)
        => _headers.TryGetValue(name, out var values) ? values! : s_emptyValues;

    /// <summary>
    /// Attempts to obtain the values for the provided header name.
    /// </summary>
    /// <param name="name">The (case-insensitive) header name.</param>
    /// <param name="values">The header's values if it exists; otherwise an empty list.</param>
    /// <returns>Whether the header exists.</returns>
    public bool TryGetValues(string name, out IReadOnlyList<string> values)
    {
        if (_headers.TryGetValue(name, out var found))
        {
            values = found!;
            return true;
        }

        values = s_emptyValues;
        return false;
    }

    /// <summary>
    /// Returns the first value for the provided header name, or <see langword="null"/> if the header does not exist.
    /// </summary>
    /// <param name="name">The (case-insensitive) header name.</param>
    /// <returns>The header's first value, or <see langword="null"/>.</returns>
    public string? GetValueOrDefault(string name)
        => _headers.TryGetValue(name, out var values) && values!.Count > 0 ? values[0] : null;

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, IReadOnlyList<string>>> GetEnumerator() => _headers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
