namespace Xhttp.Model;

internal sealed record ExpectedResponse(ResponseType Type, string? TypeName);

/// <summary>
/// The kind of response that the user expects to receive from a request method.
/// </summary>
internal enum ResponseType
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
