namespace TypedHttp.Model;

/// <summary>
/// Represents a <see cref="Request"/>'s return type.
/// </summary>
/// <param name="Kind">The kind of return type this is.</param>
/// <param name="Type">The return type's fully qualified name.</param>
/// <param name="InnerType">
/// The inner (T in Task&lt;T&gt;) return type's fully qualified name.
/// </param>
internal sealed record ReturnType(
    ReturnTypeKind Kind,
    string         Type,
    string?        InnerType)
{
    public bool NeedsUndisposedResponse => Kind is ReturnTypeKind.HttpResponseMessage or ReturnTypeKind.Stream;
}

/// <summary>
/// The kind of response that the user expects to receive from a request method.
/// </summary>
internal enum ReturnTypeKind
{
    /// <summary>
    /// The raw response, user handles everything including status code checks
    /// and response disposal.
    /// </summary>
    HttpResponseMessage,

    /// <summary>
    /// Plain string contents. Ensure success status code, read as a string and
    /// let the user handle it.
    /// </summary>
    String,

    /// <summary>
    /// Stream contents. Ensure success status code, read as stream and let the
    /// user handle it.
    /// </summary>
    Stream,

    /// <summary>
    /// User does not care about the response body. Ensure success status code
    /// and that's it.
    /// </summary>
    Void,

    /// <summary>
    /// Custom response kind. Ensure success status code and deserialize as
    /// JSON.
    /// </summary>
    Custom,
}
