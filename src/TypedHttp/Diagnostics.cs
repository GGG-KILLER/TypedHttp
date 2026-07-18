using Microsoft.CodeAnalysis;
using TypedHttp.Model;

namespace TypedHttp;

internal static class Diagnostics
{
    /// <summary>
    /// TYPEDHTTP001: Method '{0}' must have a request marker such as [Get], [Post] or similar
    /// </summary>
    public static readonly DiagnosticDescriptor NoRequestMarkerOnMethod = new(
        "TYPEDHTTP001",
        "Method has no request marker",
        "Method '{0}' must have a request marker such as [Get], [Post] or similar",
        "TypedHttp",
        DiagnosticSeverity.Error,
        true,
        "All methods must have request markers such as [Get], [Post], etc. Interfaces cannot have non-request methods.",
        customTags: [ WellKnownDiagnosticTags.NotConfigurable ]);

    /// <summary>
    /// TYPEDHTTP002: Method '{0}' should only have a single request marker such as [Get], [Post] or similar
    /// </summary>
    public static readonly DiagnosticDescriptor MultipleRequestMarkerOnMethod = new(
        "TYPEDHTTP002",
        "Method has more than one request marker",
        "Method '{0}' should only have a single request marker such as [Get], [Post] or similar",
        "TypedHttp",
        DiagnosticSeverity.Error,
        true,
        "All methods must have only a single request marker to indicate the route and HTTP method.",
        customTags: [ WellKnownDiagnosticTags.NotConfigurable ]);

    /// <summary>
    /// TYPEDHTTP003: Method '{0}' has a malformed header in [Headers]: {1}
    /// </summary>
    public static readonly DiagnosticDescriptor MalformedHeader = new(
        "TYPEDHTTP003",
        "Method has a malformed header",
        "'{0}' has a malformed header in [Headers]: {1}",
        "TypedHttp",
        DiagnosticSeverity.Error,
        true,
        "All headers should have well-formed header names and values, separated by a semicolon.",
        customTags: [ WellKnownDiagnosticTags.NotConfigurable ]);

    /// <summary>
    /// TYPEDHTTP004: Method '{0}' has a malformed route template: {1}
    /// </summary>
    public static readonly DiagnosticDescriptor MalformedRoute = new(
        "TYPEDHTTP004",
        "Method has a malformed route template",
        "Method '{0}' has a malformed route template: {1}",
        "TypedHttp",
        DiagnosticSeverity.Error,
        true,
        "Route templates should be well-formed and have properly placed opening and closing braces.",
        customTags: [ WellKnownDiagnosticTags.NotConfigurable ]);

    /// <summary>
    /// TYPEDHTTP005: Method '{0}' uses unknown parameter '{1}' in its route
    /// </summary>
    public static readonly DiagnosticDescriptor RouteHasUnkownParameter = new(
        "TYPEDHTTP005",
        "Method has a route with an unknown parameter",
        "Method '{0}' uses unknown parameter '{1}' in its route",
        "TypedHttp",
        DiagnosticSeverity.Error,
        true,
        customTags: [ WellKnownDiagnosticTags.NotConfigurable ]);

    /// <summary>
    /// TYPEDHTTP007: Method '{0}' has multiple body parameteres
    /// </summary>
    public static readonly DiagnosticDescriptor MultipleBodyParameters = new(
        "TYPEDHTTP007",
        "Method must have only one Body parameter",
        "Method '{0}' has multiple body parameters",
        "TypedHttp",
        DiagnosticSeverity.Error,
        true,
        customTags: [ WellKnownDiagnosticTags.NotConfigurable ]);

    /// <summary>
    /// TYPEDHTTP008: Method '{0}' has a return type that is not wrapped in Task, Task&lt;T&gt;, ValueTask or ValueTask&lt;T&gt;
    /// </summary>
    public static readonly DiagnosticDescriptor NonAsyncReturn = new(
        "TYPEDHTTP008",
        "Method has non-Task/ValueTask return type",
        "Method '{0}' has a return type that is not wrapped in Task, Task<T>, ValueTask or ValueTask<T>",
        "TypedHttp",
        DiagnosticSeverity.Error,
        true,
        "Non-Task return types are not supported at this point in time.",
        customTags: [ WellKnownDiagnosticTags.NotConfigurable ]);

    /// <summary>
    /// TYPEDHTTP010: Type '{0}' must be partial since it contains a client or one of its children contains a client
    /// </summary>
    public static readonly DiagnosticDescriptor NonPartialParent = new(
        "TYPEDHTTP010",
        "Type contains a client but is not partial",
        "Type '{0}' must be partial since it contains a client or one of its children contains a client",
        "TypedHttp",
        DiagnosticSeverity.Error,
        true,
        "To enable source-generation, all types containing the client must be partial.",
        customTags: [ WellKnownDiagnosticTags.NotConfigurable ]);

    // TODO: │ID          │Fixes│Condition                                                 │Severity → behavior        │
    // TODO: │TYPEDHTTP009│3.2  │interface name lacks I prefix                             │Error → skip client        │
    // TODO: │TYPEDHTTP012│9.5  │file-local [Client] interface                             │Error → skip client        │

    public static DiagnosticInfo ForMalformedRoute(Location? location, string name, TemplateError error)
    {
        return DiagnosticInfo.Create(
            MalformedRoute,
            location,
            name,
            error switch
            {
                TemplateError.UnclosedBrace => "unclosed brace in route",
                _                           => throw new ArgumentOutOfRangeException()
            });
    }

    public static DiagnosticInfo ForMalformedHeader(Location? location, string methodName, HeaderError error)
    {
        return DiagnosticInfo.Create(
            MalformedHeader,
            location,
            methodName,
            error switch
            {
                HeaderError.HeaderHasNoColon      => "header has no colon",
                HeaderError.LeftHasUnclosedBrace  => "header name has an unclosed brace",
                HeaderError.RightHasUnclosedBrace => "header value has an unclosed brace",
                _                                 => throw new ArgumentOutOfRangeException()
            });
    }
}
