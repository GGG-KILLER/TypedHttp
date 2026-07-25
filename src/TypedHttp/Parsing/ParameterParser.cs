using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TypedHttp.Model;

namespace TypedHttp.Parsing;

internal sealed class ParameterParser
{
    private readonly KnownSymbols              _knownSymbols;
    private readonly ImmutableArray<DiagnosticInfo>.Builder _diagnostics;

    public ParameterParser(
        KnownSymbols              knownSymbols,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        _knownSymbols = knownSymbols;
        _diagnostics  = diagnostics;
    }

    public Parameter Parse(
        IImmutableSet<string>                    routeParameters,
        ImmutableArray<AliasedParameter>.Builder queryParameters,
        ImmutableArray<AliasedParameter>.Builder properties,
        ImmutableArray<Header>.Builder                requestHeaders,
        IMethodSymbol                            method,
        IParameterSymbol                         parameter,
        ref RequestBody?                         body,
        ref string?                              cancellationTokenParam,
        CancellationToken                        cancellationToken = default)
    {
        var isNullable = parameter.Type.IsReferenceType
                      || parameter.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

        if (SymbolEqualityComparer.Default.Equals(parameter.Type, _knownSymbols.CancellationToken))
        {
            cancellationTokenParam = parameter.Name;
            goto end; // cancellation tokens don't need anything else
        }

        var isUsed = routeParameters.Contains(parameter.Name);

        foreach (var attribute in parameter.GetAttributes())
        {
            cancellationToken.ThrowIfCancellationRequested();

            // [Query]
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _knownSymbols.Query))
            {
                isUsed = true;
                queryParameters.Add(
                    new AliasedParameter(parameter.Name, (string)attribute.ConstructorArguments[0].Value!, isNullable));
            }

            // [Authorize]
            if (SymbolEqualityComparer.Default.Equals(
                    attribute.AttributeClass,
                    _knownSymbols.Authorize))
            {
                isUsed = true;

                var prefix = "Bearer";
                if (attribute.ConstructorArguments.Length > 0)
                {
                    prefix =
                        (string)attribute.ConstructorArguments[0].Value!;
                }

                requestHeaders.Add(
                    new Header(
                        Template.String("Authorization"),
                        new Template(
                            [
                                new TemplatePart(
                                    TemplatePartKind
                                       .String,
                                    $"{prefix} "),
                                new TemplatePart(
                                    TemplatePartKind
                                       .Parameter,
                                    parameter.Name)
                            ])));
            }

            // [Header]
            if (SymbolEqualityComparer.Default.Equals(
                    attribute.AttributeClass,
                    _knownSymbols.Header))
            {
                isUsed = true;

                var name = (string)attribute.ConstructorArguments[0].Value!;
                requestHeaders.Add(
                    new Header(
                        Template.String(name),
                        Template.Parameter(parameter.Name)));
            }

            // [Property] and [Property(str)]
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _knownSymbols.Property))
            {
                isUsed = true;

                var propertyName = parameter.Name;
                if (attribute.ConstructorArguments.Length > 0)
                {
                    propertyName = (string)attribute.ConstructorArguments[0].Value!;
                }
                properties.Add(new AliasedParameter(parameter.Name, propertyName, isNullable));
            }

            // [Body]
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _knownSymbols.Body))
            {
                isUsed = true;

                if (body is not null)
                {
                    _diagnostics.Add(
                        DiagnosticInfo.Create(
                            Diagnostics.MultipleBodyParameters,
                            method.Locations.FirstOrDefault(),
                            method.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
                }
                else
                {
                    body = new RequestBody(
                        parameter.Type.ToDisplayString(SymbolDisplayFormats.FullTypeFormat),
                        parameter.Name,
                        GetBodyKind(parameter.Type));
                }
            }
        }

        // If a parameter is unused, we simply forward it as a query parameter.
        if (!isUsed)
        {
            queryParameters.Add(new AliasedParameter(parameter.Name, parameter.Name, isNullable));
        }

    end:
        return new Parameter(
            IsNullable: isNullable,
            Type: parameter.Type.ToDisplayString(SymbolDisplayFormats.FullTypeFormat),
            Name: parameter.Name);
    }

    private BodyKind GetBodyKind(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String) return BodyKind.String;

        if (type.InheritsFrom(_knownSymbols.HttpContent)) return BodyKind.HttpContent;

        if (type.InheritsFrom(_knownSymbols.Stream)) return BodyKind.Stream;

        return BodyKind.Json;
    }
}
