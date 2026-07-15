using JetBrains.Annotations;

namespace TypedHttp;

/// <summary>
/// The exception thrown when an HTTP response's status code did not indicate success (2xx).
/// Unlike <see cref="HttpRequestException"/>, the response snapshot (status code, reason
/// phrase and headers) is available through <see cref="Response"/>.
/// </summary>
[PublicAPI]
public sealed class UnsuccessfulResponseException : HttpRequestException
{
    /// <summary>
    /// The snapshot of the response that did not indicate success.
    /// </summary>
    public Response Response { get; }

    /// <summary>
    /// Initializes a new exception for the provided response snapshot.
    /// </summary>
    /// <param name="response">The snapshot of the response that did not indicate success.</param>
    public UnsuccessfulResponseException(Response response)
        : base(BuildMessage(response))
    {
        Response = response;
    }

    private static string BuildMessage(Response response)
    {
        if (response is null) throw new ArgumentNullException(nameof(response));
        return
            $"Response status code does not indicate success: {(int)response.StatusCode}{(response.ReasonPhrase is null ? "" : $" ({response.ReasonPhrase})")}.";
    }
}
