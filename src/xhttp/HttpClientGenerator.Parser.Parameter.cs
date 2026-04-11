using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Xhttp.Model;

namespace Xhttp;

public partial class HttpClientGenerator
{
    private sealed partial class Parser
    {
        public Parameter ParseParameter(
            IImmutableSet<string>          routeParameters,
            ImmutableArray<Header>.Builder requestHeaders,
            IParameterSymbol               parameter)
        {
            ParameterKind parameterKind;
            string?       propertyName = null, alias = null;
            var           isString     = parameter.Type.SpecialType == SpecialType.System_String;

            if (SymbolEqualityComparer.Default.Equals(parameter.Type,
                                                      _knownSymbols.CancellationToken))
                parameterKind = ParameterKind.CancellationToken;
            else if (routeParameters.Contains(parameter.Name))
                parameterKind = isString ? ParameterKind.StringRoute : ParameterKind.NonStringRoute;
            else
                parameterKind = isString ? ParameterKind.StringQuery : ParameterKind.NonStringQuery;

            foreach (var attribute in parameter.GetAttributes())
            {
                _cancellationToken.ThrowIfCancellationRequested();

                // [AliasAs]
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass,
                                                          _knownSymbols.Alias))
                {
                    alias = (string)attribute.ConstructorArguments[0].Value!;
                }

                // [Authorize]
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass,
                                                          _knownSymbols.Authorize))
                {
                    var prefix = "Bearer";
                    if (attribute.ConstructorArguments.Length > 0)
                    {
                        prefix =
                            (string)attribute.ConstructorArguments[0].Value!;
                    }

                    requestHeaders.Add(new Header(Template.String("Authorization"),
                                                  new Template([
                                                                   new TemplatePart(TemplatePartKind
                                                                          .String,
                                                                       $"{prefix} "),
                                                                   new TemplatePart(TemplatePartKind
                                                                          .Parameter,
                                                                       parameter.Name)
                                                               ])));
                    parameterKind = ParameterKind.Ignore;
                }

                // [Header]
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass,
                                                          _knownSymbols.Header))
                {
                    var name = (string)attribute.ConstructorArguments[0].Value!;
                    requestHeaders.Add(new Header(Template.String(name),
                                                  Template.Parameter(parameter.Name)));
                    parameterKind = ParameterKind.Ignore;
                }

                // [Property] and [Property(str)]
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass,
                                                          _knownSymbols.Property))
                {
                    if (attribute.ConstructorArguments.Length > 0)
                    {
                        propertyName =
                            (string)attribute.ConstructorArguments[0].Value!;
                    }

                    // TODO: Warn about multiple usage.
                    parameterKind = isString ? ParameterKind.StringProperty : ParameterKind.NonStringProperty;
                }

                // [Body]
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass,
                                                          _knownSymbols.Body))
                {
                    // TODO: Warn about different previous usage.
                    parameterKind = ParseBodyType(parameter.Type);
                }
            }

            return new Parameter(IsNullable: parameter.Type.IsReferenceType
                                          || parameter.Type.SpecialType == SpecialType.System_Nullable_T,
                                 Type: parameter.Type.ToDisplayString(s_fullTypeFormat),
                                 Name: parameter.Name,
                                 Kind: parameterKind,
                                 PropertyName: propertyName,
                                 Alias: alias);
        }

        private ParameterKind ParseBodyType(ITypeSymbol type)
        {
            if (type.SpecialType == SpecialType.System_String) return ParameterKind.StringBody;

            if (type.InheritsFrom(_knownSymbols.HttpContent)) return ParameterKind.HttpContentBody;

            if (type.InheritsFrom(_knownSymbols.Stream)) return ParameterKind.StreamBody;

            return ParameterKind.JsonBody;
        }
    }
}
