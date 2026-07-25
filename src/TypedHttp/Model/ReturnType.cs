namespace TypedHttp.Model;

internal abstract record ReturnType(string FullType)
{
    public virtual bool NeedsUndisposedResponse => false;
}

/// <summary>
/// The raw response, user handles everything including status code checks
/// and response disposal.
/// </summary>
internal sealed record HttpResponseMessageReturnType(string FullType) : ReturnType(FullType)
{
    /// <inheritdoc />
    public override bool NeedsUndisposedResponse => true;
}

/// <summary>
/// Our own specialized Response type.
/// </summary>
internal sealed record ResponseReturnType(string FullType) : ReturnType(FullType);

/// <summary>
/// Our own specialized Response&lt;T&gt; type.
/// </summary>
internal sealed record ResponseOfTReturnType(string FullType, string DeserializeType) : ReturnType(FullType);

/// <summary>
/// Plain string contents. Ensure success status code, read as a string and
/// let the user handle it.
/// </summary>
internal sealed record StringReturnType(string FullType) : ReturnType(FullType);

/// <summary>
/// Stream contents. Ensure success status code, read as stream and let the
/// user handle it.
/// </summary>
internal sealed record StreamReturnType(string FullType) : ReturnType(FullType)
{
    /// <inheritdoc />
    public override bool NeedsUndisposedResponse => true;
}

/// <summary>
/// User does not care about the response body. Ensure success status code
/// and that's it.
/// </summary>
internal sealed record VoidReturnType(string FullType) : ReturnType(FullType);

/// <summary>
/// Custom response kind. Ensure success status code and deserialize as
/// JSON.
/// </summary>
internal sealed record CustomReturnType(string FullType, string DeserializeType) : ReturnType(FullType);
