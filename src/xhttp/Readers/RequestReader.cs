using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Xhttp.Model;

namespace Xhttp.Readers;

internal static class RequestReader
{
    public static Request ReadRequest(
        GeneratorAttributeSyntaxContext context,
        CancellationToken               cancellationToken)
    {
        var attribute = context.Attributes.Single();
        var method = attribute.AttributeClass!.MetadataName[
            "Xhttp.".Length..^"Attributes".Length];

        string route;
        // Read the [Request(method, route)] format if it's a request attribute
        if (string.Equals("Request",
                          method,
                          StringComparison.OrdinalIgnoreCase))
        {
            method = (string)attribute.ConstructorArguments[0].Value!;
            route  = (string)attribute.ConstructorArguments[1].Value!;
        }
        else // Otherwise we just have the route
            route = (string)attribute.ConstructorArguments[0].Value!;

        // Convert to all caps so it's easier
        method = method.ToUpperInvariant();

        var methodSymbol = (IMethodSymbol)context.TargetSymbol;

        var containingStructure =
            ExtractContainingStructure(methodSymbol, cancellationToken);
        var expectedResponse =
            ExtractExpectedResponse(methodSymbol, cancellationToken);
        var requestHeaders = ExtractHeaders(methodSymbol, cancellationToken);
        ProcessParameters(methodSymbol.Parameters,
                          cancellationToken,
                          out var aliases,
                          out var properties,
                          out var parametersHeaders,
                          out var body);

        var headers = new ImmutableByValArray<Header>(
            [ ..requestHeaders, ..parametersHeaders ]);

        return new Request(containingStructure,
                           method,
                           Template.Parse(route),
                           aliases,
                           properties,
                           headers,
                           body,
                           expectedResponse);
    }

    private static ImmutableByValArray<ContainingStructure>
        ExtractContainingStructure(
            IMethodSymbol     method,
            CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<ContainingStructure>();

        var type = method.ContainingType!;
        builder.Add(new ContainingStructure(type.TypeKind == TypeKind.Class
                                                ? StructureKind.Class
                                                : StructureKind.Struct,
                                            type.Name));

        while (type.ContainingType != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            type = type.ContainingType;
            builder.Add(new ContainingStructure(type.TypeKind == TypeKind.Class
                                                    ? StructureKind.Class
                                                    : StructureKind.Struct,
                                                type.Name));
        }

        if (type.ContainingNamespace != null)
        {
            var ns = type.ContainingNamespace!.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat
                                   .WithGlobalNamespaceStyle(
                                        SymbolDisplayGlobalNamespaceStyle
                                           .Omitted));
            builder.Add(new ContainingStructure(StructureKind.Namespace, ns));
        }

        builder.Reverse();
        return new ImmutableByValArray<ContainingStructure>(
            builder.ToImmutable());
    }

    private static ReturnType ExtractExpectedResponse(
        IMethodSymbol     method,
        CancellationToken cancellationToken)
    {
        TODO
    }

    private static ImmutableByValArray<Header> ExtractHeaders(
        IMethodSymbol     method,
        CancellationToken cancellationToken)
    {
        var headers = ImmutableArray.CreateBuilder<Header>();

        foreach (var attribute in method.GetAttributes())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (attribute.AttributeClass?.MetadataName != MetadataNames.Headers)
                continue;

            foreach (var raw in attribute.ConstructorArguments)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var str = (string)raw.Value!;

                var idx = str.IndexOf(':');
                if (idx == -1) continue;

                var name  = Template.Parse(str[..idx].Trim());
                var value = Template.Parse(str[(idx + 1)..].Trim());
                headers.Add(new Header(name,
                                       value));
            }
        }

        return new
            ImmutableByValArray<Header>(headers.ToImmutable());
    }

    private static void ProcessParameters(
        ImmutableArray<IParameterSymbol>             parameters,
        CancellationToken                            cancellationToken,
        out ImmutableByValDictionary<string, string> aliases,
        out ImmutableByValArray<Property>            properties,
        out ImmutableByValArray<Header>              headers,
        out Body?                                    body)
    {
        var aliasesBuilder =
            ImmutableDictionary.CreateBuilder<string, string>();
        var propsBuilder =
            ImmutableArray.CreateBuilder<Property>();
        var headersBuilder = ImmutableArray
           .CreateBuilder<Header>();
        var bodyBox = new Box<Body?>();
        var reader =
            new ParameterReader(aliasesBuilder,
                                propsBuilder,
                                headersBuilder,
                                bodyBox);

        foreach (var parameter in parameters)
        {
            cancellationToken.ThrowIfCancellationRequested();
            reader.ProcessParameter(parameter, cancellationToken);
        }

        aliases =
            new ImmutableByValDictionary<string, string>(
                aliasesBuilder.ToImmutable());
        properties =
            new ImmutableByValArray<Property>(propsBuilder.ToImmutable());
        headers = new ImmutableByValArray<Header>(headersBuilder.ToImmutable());
        body    = bodyBox.Value;
    }
}
