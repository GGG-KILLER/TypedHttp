namespace TypedHttp.Model;

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
/// <param name="QueryParameterName">
/// Name that the parameter will be sent under in the query string.
/// </param>
internal sealed record Parameter(
    bool          IsNullable,
    string        Type,
    string        Name,
    ParameterKind Kind,
    string?       PropertyName       = null,
    string?       QueryParameterName = null)
{
    public bool IsRoute    => Kind is ParameterKind.NonStringRoute or ParameterKind.StringRoute;
    public bool IsQuery    => Kind is ParameterKind.NonStringQuery or ParameterKind.StringQuery;
    public bool IsProperty => Kind is ParameterKind.NonStringProperty or ParameterKind.StringProperty;

    public bool IsBody
        => Kind is ParameterKind.JsonBody
                or ParameterKind.StreamBody
                or ParameterKind.StringBody
                or ParameterKind.HttpContentBody;
}

internal enum ParameterKind
{
    /// <summary>
    /// Parameter a string to be used in the route.
    /// </summary>
    StringRoute,

    /// <summary>
    /// Parameter not a string to be used in the route.
    /// </summary>
    NonStringRoute,

    /// <summary>
    /// Parameter is to be stored as a property.
    /// </summary>
    StringProperty,

    /// <summary>
    /// Parameter is to be stored as a property.
    /// </summary>
    NonStringProperty,

    /// <summary>
    /// Parameter is to be used in the query string.
    /// </summary>
    StringQuery,

    /// <summary>
    /// Parameter is to be used in the query string.
    /// </summary>
    NonStringQuery,

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
    /// Parameter being used for other means (header, etc.)
    /// </summary>
    Ignore,

    /// <summary>
    /// The <see cref="CancellationToken"/> to use for all async operations in
    /// the request flow.
    /// </summary>
    CancellationToken,
}
