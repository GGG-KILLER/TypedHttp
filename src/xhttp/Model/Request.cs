using Microsoft.CodeAnalysis;

namespace Xhttp.Model;

/// <summary>
/// Represents an entire endpoint method.
/// </summary>
/// <param name="Name">
/// The name (including type parameters) of the method that will execute the request.
/// </param>
/// <param name="Method">
/// The HTTP method to use when calling the endpoint.
/// </param>
/// <param name="Route">
/// The template for endpoint path.
/// </param>
/// <param name="Headers">
/// Headers to be added to <see cref="HttpRequestMessage.Headers"/>
/// </param>
/// <param name="Parameters">
/// The method's parameters.
/// </param>
/// <param name="ReturnType">
/// The method's return type.
/// </param>
internal sealed record Request(
    string                          Name,
    string                          Method,
    Template                        Route,
    ImmutableByValArray<Header>     Headers,
    ImmutableByValArray<Parameter>  Parameters,
    ReturnType                      ReturnType,
    ImmutableByValArray<Diagnostic> Diagnostics);
