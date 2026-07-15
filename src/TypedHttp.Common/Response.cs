using System.Net;
using JetBrains.Annotations;

namespace TypedHttp;

/// <summary>
/// A materialized snapshot of an HTTP response: status code, reason phrase and headers.
/// Unlike <see cref="HttpResponseMessage"/>, it holds no connection or stream and therefore
/// does not need to be disposed.
/// </summary>
[PublicAPI]
public class Response
{
    /// <summary>
    /// The status code of the response.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// The reason phrase sent by the server along with the status code, if any.
    /// </summary>
    public string? ReasonPhrase { get; }

    /// <summary>
    /// A snapshot of the response's headers, including content headers.
    /// </summary>
    public ResponseHeaders Headers { get; }

    /// <summary>
    /// Whether the response's status code indicates success (2xx).
    /// </summary>
    public bool IsSuccessStatusCode => (int)StatusCode is >= 200 and <= 299;

    /// <summary>
    /// Initializes a new response snapshot.
    /// </summary>
    /// <param name="statusCode">The status code of the response.</param>
    /// <param name="reasonPhrase">The reason phrase sent by the server, if any.</param>
    /// <param name="headers">A snapshot of the response's headers.</param>
    public Response(HttpStatusCode statusCode, string? reasonPhrase, ResponseHeaders headers)
    {
        StatusCode   = statusCode;
        ReasonPhrase = reasonPhrase;
        Headers      = headers ?? throw new ArgumentNullException(nameof(headers));
    }

    /// <summary>
    /// Throws an <see cref="UnsuccessfulResponseException"/> if <see cref="IsSuccessStatusCode"/> is
    /// <see langword="false"/>.
    /// </summary>
    /// <returns>This response, for chaining.</returns>
    /// <exception cref="UnsuccessfulResponseException">The status code did not indicate success (2xx).</exception>
    public Response EnsureSuccessStatusCode()
    {
        if (!IsSuccessStatusCode) throw new UnsuccessfulResponseException(this);
        return this;
    }

    /// <summary>
    /// Creates a snapshot of the provided message. The message can be safely disposed afterwards.
    /// </summary>
    /// <param name="message">The message to snapshot.</param>
    /// <returns>The created snapshot.</returns>
    public static Response FromMessage(HttpResponseMessage message)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));
        return new Response(message.StatusCode, message.ReasonPhrase, ResponseHeaders.FromMessage(message));
    }
}

/// <summary>
/// A materialized snapshot of an HTTP response with a deserialized body.
/// Unlike <see cref="HttpResponseMessage"/>, it holds no connection or stream and therefore
/// does not need to be disposed.
/// </summary>
/// <typeparam name="T">The type the response body is deserialized into.</typeparam>
[PublicAPI]
public sealed class Response<T> : Response
{
    /// <summary>
    /// The deserialized response body, or <see langword="default"/> when no body was read
    /// (e.g. the status code did not indicate success).
    /// </summary>
    public T? Body { get; }

    /// <summary>
    /// Initializes a new response snapshot.
    /// </summary>
    /// <param name="statusCode">The status code of the response.</param>
    /// <param name="reasonPhrase">The reason phrase sent by the server, if any.</param>
    /// <param name="headers">A snapshot of the response's headers.</param>
    /// <param name="body">The deserialized response body, if any.</param>
    public Response(HttpStatusCode statusCode, string? reasonPhrase, ResponseHeaders headers, T? body)
        : base(statusCode, reasonPhrase, headers)
    {
        Body = body;
    }

    /// <summary>
    /// Throws an <see cref="UnsuccessfulResponseException"/> if <see cref="Response.IsSuccessStatusCode"/>
    /// is <see langword="false"/>.
    /// </summary>
    /// <returns>This response, for chaining.</returns>
    /// <exception cref="UnsuccessfulResponseException">The status code did not indicate success (2xx).</exception>
    public new Response<T> EnsureSuccessStatusCode()
    {
        base.EnsureSuccessStatusCode();
        return this;
    }

    /// <summary>
    /// Creates a snapshot of the provided message with the provided deserialized body.
    /// The message can be safely disposed afterwards.
    /// </summary>
    /// <param name="message">The message to snapshot.</param>
    /// <param name="body">The deserialized response body, if any.</param>
    /// <returns>The created snapshot.</returns>
    public static Response<T> FromMessage(HttpResponseMessage message, T? body)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));
        return new Response<T>(message.StatusCode, message.ReasonPhrase, ResponseHeaders.FromMessage(message), body);
    }
}
