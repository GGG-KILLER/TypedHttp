namespace TypedHttp.Model;

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
    string                          Interface,
    string                          ConstraintClauses,
    ImmutableByValArray<Header>     Headers,
    ImmutableByValArray<Request>    Requests,
    ImmutableByValArray<DiagnosticInfo> Diagnostics)
{
    /// <summary>
    /// The generated class name as it appears in the declaration, including any generic type
    /// parameter list (e.g. <c>CrudClient</c> or <c>GenericClient&lt;T&gt;</c>).
    /// </summary>
    public string Name { get; } = Interface.Substring(1);

    /// <summary>
    /// The bare class identifier without any type parameter list, for use where type parameters
    /// are illegal: constructor names and the generated file's hint name.
    /// </summary>
    public string Identifier { get; } = StripTypeParameters(Interface.Substring(1));

    private static string StripTypeParameters(string name)
    {
        var index = name.IndexOf('<');
        return index < 0 ? name : name.Substring(0, index);
    }
}
