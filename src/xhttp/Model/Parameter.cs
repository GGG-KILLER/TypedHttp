namespace Xhttp.Model;

/// <summary>
/// Represents a request parameter.
/// </summary>
/// <param name="Type">
/// The parameter's type (includes type parameters).
/// </param>
/// <param name="Name">
/// The parameter's name.
/// </param>
/// <param name="Kind">
/// The parameter's kind (purpose/usage).
/// </param>
/// <param name="PropertyName">
/// The name of the property the parameter should be stored under.
/// </param>
/// <param name="Alias">
/// An alias that the parameter can be referred as (must be used as the query
/// string key).
/// </param>
internal readonly record struct Parameter(
    string        Type,
    string        Name,
    ParameterKind Kind,
    string?       PropertyName = null,
    string?       Alias        = null);

internal enum ParameterKind
{
    /// <summary>
    /// Parameter is to be used in the route.
    /// </summary>
    Route,

    /// <summary>
    /// Parameter is to be stored as a property.
    /// </summary>
    Property,

    /// <summary>
    /// Parameter is to be used in the query string.
    /// </summary>
    Query,

    /// <summary>
    /// Parameter is an <see cref="HttpContent"/> derived type to be used as the
    /// raw request contents.
    /// </summary>
    HttpContentBody,

    /// <summary>
    /// Parameter is a <see cref="Stream"/> derived type to be used as the
    /// raw request body.
    /// </summary>
    StreamBody,

    /// <summary>
    /// Parameter is a <see cref="string"/> to be used as the raw request body.
    /// </summary>
    StringBody,

    /// <summary>
    /// Parameter is to be used as a JSON-encoded body.
    /// </summary>
    JsonBody,

    /// <summary>
    /// The <see cref="CancellationToken"/> to use for all async operations in
    /// the request flow.
    /// </summary>
    CancellationToken,
}
