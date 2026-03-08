using Microsoft.CodeAnalysis;

namespace Xhttp.Model;

/// <summary>
/// Represents an HTTP Client to be generated.
/// </summary>
/// <param name="Containers">
/// The containing namespaces, types and the client itself in that order.
/// </param>
/// <param name="Headers">
/// Client-wide headers.
/// </param>
/// <param name="Requests">
/// Client requests.
/// </param>
/// <param name="Diagnostics">
/// Client diagnostics (errors and warnings).
/// </param>
internal sealed record Client(
    ImmutableByValArray<string>     Containers,
    string                          Modifiers,
    string                          Name,
    ImmutableByValArray<Header>     Headers,
    ImmutableByValArray<Request>    Requests,
    ImmutableByValArray<Diagnostic> Diagnostics);
