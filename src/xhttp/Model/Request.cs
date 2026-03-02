namespace Xhttp.Model;

/// <summary>
/// Represents an entire endpoint method.
/// </summary>
/// <param name="ContainingStructures">
/// The structures (namespace, class(es), struct(s)) that this request method is
/// contained within.
/// </param>
/// <param name="Route">
/// The template for the route that will be sent on the request.
/// </param>
/// <param name="Aliases">
/// Alias name to parameter name lookup table.
/// </param>
/// <param name="Properties">
/// Properties to be added to <see cref="HttpRequestMessage.Properties"/>.
/// </param>
/// <param name="Headers">
/// Headers to be added to <see cref="HttpRequestMessage.Headers"/>
/// </param>
/// <param name="Body">
/// The body to be sent together with the request.
/// </param>
/// <param name="ExpectedResponse">
/// The expected response from the method's signature.
/// </param>
internal sealed record Request(
    ImmutableByValArray<ContainingStructure> ContainingStructures,
    string                                   Method,
    Template                                 Route,
    ImmutableByValDictionary<string, string> Aliases,
    ImmutableByValArray<Property>            Properties,
    ImmutableByValArray<Header>              Headers,
    Body?                                    Body,
    ExpectedResponse                         ExpectedResponse);

internal readonly struct Property(string Key, string Value);

internal readonly struct Header(Template Name, Template Value);
