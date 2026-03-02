namespace Xhttp.Model;

/// <summary>
/// Represents the body sent in a request.
/// </summary>
/// <param name="Kind">
/// The kind of body being sent, to choose the serialization mode.
/// </param>
/// <param name="ParameterName">
/// The name of the parameter that contains the body contents.
/// </param>
internal sealed record Body(BodyKind Kind, string ParameterName);

/// <summary>
/// Represents the type of body sent in a request.
/// </summary>
internal enum BodyKind
{
    /// <summary>
    /// The body content is an <see cref="HttpContent"/> descendant, therefore
    /// no serialization needed, just put it in
    /// <see cref="HttpRequestMessage.Content"/>.
    /// </summary>
    HttpContent,

    /// <summary>
    /// The body content is a <see cref="Stream"/>, so no serialization is
    /// needed, only stream it directly to the remote server.
    /// </summary>
    Stream,

    /// <summary>
    /// The body content is a <see cref="string"/>, so no serialization or
    /// encoding is needed, only send it as-is to the remote server.
    /// </summary>
    String,

    /// <summary>
    /// The body content is not an <see cref="HttpContent"/>, <see cref="Stream"/>
    /// or a <see cref="string"/>, so we put it into a JsonContent and send it.
    /// </summary>
    Json
}
