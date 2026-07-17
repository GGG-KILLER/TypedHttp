using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TypedHttp.Model;

namespace TypedHttp.Parsing;

internal sealed class RequestParser
{
    private readonly KnownSymbols              _knownSymbols;
    private readonly ImmutableArray<DiagnosticInfo>.Builder _diagnostics;

    public RequestParser(
        KnownSymbols              knownSymbols,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        _knownSymbols  = knownSymbols;
        _diagnostics   = diagnostics;
    }

    public Request? TryParse(
        IMethodSymbol     method,
        CancellationToken cancellationToken = default)
    {
        var attributes = method.GetAttributes()
                               .ToLookup(attr => attr.AttributeClass!, SymbolEqualityComparer.Default);

        var reqIds =
            attributes
#pragma warning disable RS1024
                // The ImmutableHashSet<T> already uses the comparer
               .Where(x => _knownSymbols.RequestMarkers.Contains(x.Key!))
#pragma warning restore RS1024
               .SelectMany(x => x)
               .ToImmutableArray();

        if (reqIds.Length == 0)
        {
            _diagnostics.Add(
                DiagnosticInfo.Create(
                    Diagnostics.NoRequestMarkerOnMethod,
                    method.Locations.FirstOrDefault(),
                    method.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
            return null;
        }
        if (reqIds.Length > 1)
        {
            _diagnostics.Add(
                DiagnosticInfo.Create(
                    Diagnostics.MultipleRequestMarkerOnMethod,
                    method.Locations.FirstOrDefault(),
                    method.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
            return null;
        }
        var reqId = reqIds[0];

        string   httpMethod;
        Template route;

        if (SymbolEqualityComparer.Default.Equals(reqId.AttributeClass, _knownSymbols.Request))
        {
            httpMethod = (string)reqId.ConstructorArguments[0].Value!;
            route      = parseRoute((string)reqId.ConstructorArguments[1].Value!);
        }
        else
        {
            httpMethod = reqId.AttributeClass!.Name;
            httpMethod = httpMethod.Substring(0, httpMethod.IndexOf("Attribute", StringComparison.Ordinal));
            route      = parseRoute((string)reqId.ConstructorArguments[0].Value!);
        }

        var headers = ParseRequestHeaders(method, attributes[_knownSymbols.Headers], cancellationToken);

        var          routeParameters            = GetRouteParameters(route);
        var          parameters                 = ImmutableArray.CreateBuilder<Parameter>();
        var          queryParameters            = ImmutableArray.CreateBuilder<AliasedParameter>();
        var          properties                 = ImmutableArray.CreateBuilder<AliasedParameter>();
        var          parameterParser            = new ParameterParser(_knownSymbols, _diagnostics);
        RequestBody? body                       = null;
        string?      cancellationTokenParameter = null;
        foreach (var parameter in method.Parameters)
        {
            cancellationToken.ThrowIfCancellationRequested();
            parameters.Add(
                parameterParser.Parse(
                    routeParameters,
                    queryParameters,
                    properties,
                    headers,
                    method,
                    parameter,
                    ref body,
                    ref cancellationTokenParameter,
                    cancellationToken));
        }

        // Every route placeholder must be filled by a method parameter.
        if (!routeParameters.IsEmpty)
        {
            var parameterNames = method.Parameters
                                       .Select(p => p.Name)
                                       .ToImmutableHashSet(StringComparer.Ordinal);
            foreach (var routeParameter in routeParameters)
            {
                if (parameterNames.Contains(routeParameter)) continue;

                _diagnostics.Add(
                    DiagnosticInfo.Create(
                        Diagnostics.RouteHasUnkownParameter,
                        reqId.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation(),
                        method.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
                        routeParameter));
            }
        }

        var returnType = new ReturnTypeParser(_knownSymbols, _diagnostics).Parse(method);

        return new Request(
            Name: GetMethodName(method),
            Method: httpMethod,
            Headers: headers.DrainToImmutable().ByVal(),
            Route: route,
            QueryParameters: queryParameters.DrainToImmutable().ByVal(),
            Properties: properties.DrainToImmutable().ByVal(),
            Body: body,
            Parameters: parameters.DrainToImmutable().ByVal(),
            CancellationTokenParameter: cancellationTokenParameter,
            ReturnType: returnType);

        Template parseRoute(string rawTemplate)
        {
            var parsedRoute = Template.Parse(rawTemplate);
            if (parsedRoute is { IsErr: true, Err.Value: var err })
            {
                _diagnostics.Add(
                    Diagnostics.ForMalformedRoute(
                        reqId.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation(),
                        method.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
                        err));
                return Template.String("");
            }
            else
            {
                return parsedRoute.Ok.Value;
            }
        }
    }

    private static readonly SymbolDisplayFormat s_methodNameFormat =
        Sdf.FullTypeFormat.WithMemberOptions(SymbolDisplayMemberOptions.None);

    private static string GetMethodName(IMethodSymbol method) => method.ToDisplayString(s_methodNameFormat);

    private ImmutableArray<Header>.Builder ParseRequestHeaders(
        IMethodSymbol              method,
        IEnumerable<AttributeData> attributes,
        CancellationToken          cancellationToken = default)
    {
        var builder = ImmutableArray.CreateBuilder<Header>();

        foreach (var attribute in attributes)
        {
            // Ignore empty [Headers] or ones with non-array arguments
            if (attribute.ConstructorArguments.Length  < 1
             || attribute.ConstructorArguments[0].Kind != TypedConstantKind.Array)
                continue;

            foreach (var rawHeader in attribute.ConstructorArguments[0].Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var headerStr = (string)rawHeader.Value!;
                var parsed    = Header.Parse(headerStr);
                if (parsed is { IsErr: true, Err.Value: var err })
                {
                    _diagnostics.Add(
                        Diagnostics.ForMalformedHeader(
                            attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                            method.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
                            err));
                }
                else
                {
                    builder.Add(parsed.Ok.Value);
                }
            }
        }

        return builder;
    }

    private static ImmutableHashSet<string> GetRouteParameters(Template route)
    {
        return route.Parts
                    .Where(p => p.Kind is TemplatePartKind.Parameter)
                    .Select(p => p.Value)
                    .ToImmutableHashSet(StringComparer.Ordinal);
    }
}
