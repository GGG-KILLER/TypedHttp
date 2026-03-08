using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xhttp.Model;

namespace Xhttp.Readers;

internal readonly struct ParameterReader(
    ImmutableDictionary<string, string>.Builder aliases,
    ImmutableArray<Property>.Builder            properties,
    ImmutableArray<Header>.Builder              headers,
    Box<Body?>                                  body)
{
    public void ProcessParameter(
        IParameterSymbol  parameter,
        CancellationToken cancellationToken)
    {
        var parameterDisplayName =
            parameter.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var attributes = parameter.GetAttributes();

        foreach (var attribute in attributes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (attribute.AttributeClass?.MetadataName)
            {
                case MetadataNames.Alias:
                    aliases[(string)attribute.ConstructorArguments[0].Value!] =
                        parameterDisplayName;
                    break;

                case MetadataNames.Body:
                    body.Value =
                        new Body(GetBodyKind(parameter), parameterDisplayName);
                    break;

                case MetadataNames.Header:
                    headers.Add(
                        CreateGenericHeader(attribute, parameterDisplayName));
                    break;

                case MetadataNames.Property:
                    properties.Add(
                        CreateProperty(attribute, parameterDisplayName));
                    break;

                case MetadataNames.Authorize:
                    headers.Add(CreateAuthorizationHeader(attribute,
                                                          parameterDisplayName));
                    break;
            }
        }
    }

    private static Header CreateGenericHeader(
        AttributeData attribute,
        string        parameterDisplayName)
    {
        var key = Template.Parse(
            (string)attribute.ConstructorArguments[0].Value!);

        var value = Template.Parameter(parameterDisplayName);

        return new Header(key, value);
    }

    private static Property CreateProperty(
        AttributeData attribute,
        string        parameterDisplayName)
    {
        // If the key is not defined, we're to use the parameter name
        var key = attribute.ConstructorArguments.Length > 0
                      ? (string)attribute.ConstructorArguments[0].Value!
                      : parameterDisplayName;

        return new Property(key, parameterDisplayName);
    }

    private static Header CreateAuthorizationHeader(
        AttributeData attribute,
        string        parameterDisplayName)
    {
        // Schema is optional, so fallback to bearer if we don't have any
        var schema = attribute.ConstructorArguments.Length > 0
                         ? (string)attribute.ConstructorArguments[0].Value!
                         : "Bearer";

        // Value is "{schema} {parameter}"
        var value =
            $"{SymbolDisplay.FormatLiteral(schema, false)} {{{parameterDisplayName}}}";

        return new Header(Template.String("Authorization"),
                          new Template(TemplateKind.Interpolation,
                                       value));
    }

    private static BodyKind GetBodyKind(IParameterSymbol parameter)
    {
        // String fast track
        if (parameter.Type.MetadataName == "System.String")
            return BodyKind.String;

        // Check if it inherits from HttpContent or Stream at any point
        var type = parameter.Type;
        do
        {
            if (type.MetadataName == "System.Net.Http.HttpContent")
                return BodyKind.HttpContent;

            if (type.MetadataName == "System.IO.Stream") return BodyKind.Stream;

            type = type.BaseType;
        } while (type != null);

        // Otherwise, fallback to JSON encoding
        return BodyKind.Json;
    }
}
