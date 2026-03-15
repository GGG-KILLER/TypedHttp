using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Xhttp.Model;

namespace Xhttp;

public partial class HttpClientGenerator
{
    private sealed partial class Parser
    {
        public Request? TryParseRequest(IMethodSymbol method)
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
            if (reqIds.Length != 1) return null; // TODO: Diagnostic about multiple markers
            var reqId = reqIds[0];

            string   httpMethod;
            Template route;

            if (SymbolEqualityComparer.Default.Equals(reqId.AttributeClass, _knownSymbols.Request))
            {
                // TODO: Validate method.
                httpMethod = (string)reqId.ConstructorArguments[0].Value!;
                route      = Template.Parse((string)reqId.ConstructorArguments[1].Value!);
            }
            else
            {
                httpMethod = reqId.AttributeClass!.Name;
                httpMethod = httpMethod.Substring(0, httpMethod.IndexOf("Attribute", StringComparison.Ordinal));
                route      = Template.Parse((string)reqId.ConstructorArguments[0].Value!);
            }

            var headers = ParseRequestHeaders(attributes[_knownSymbols.Headers]);

            var routeParameters = GetRouterParameters(route);
            var parameters      = ImmutableDictionary.CreateBuilder<string, Parameter>(StringComparer.Ordinal);
            foreach (var parameter in method.Parameters)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                parameters.Add(parameter.Name, ParseParameter(routeParameters, headers, parameter));
            }

            var returnType = ParseReturnType(method);

            return new Request(Name: GetMethodName(method),
                               Method: httpMethod,
                               Route: route,
                               Headers: headers.DrainToImmutable().ByVal(),
                               Parameters: parameters.ToImmutable().ByVal(),
                               ReturnType: returnType,
                               Diagnostics: ImmutableArray<Diagnostic>.Empty.ByVal());
        }

        private static readonly SymbolDisplayFormat s_methodNameFormat =
            s_fullTypeFormat!.WithMemberOptions(SymbolDisplayMemberOptions.None);

        private static string GetMethodName(IMethodSymbol method) => method.ToDisplayString(s_methodNameFormat);

        private static ImmutableHashSet<string> GetRouterParameters(Template route)
        {
            return route.Parts
                        .Where(p => p.Kind is TemplatePartKind.Parameter)
                        .Select(p => p.Value)
                        .ToImmutableHashSet(StringComparer.Ordinal);
        }

        private ImmutableArray<Header>.Builder ParseRequestHeaders(IEnumerable<AttributeData> attributes)
        {
            var builder =
                ImmutableArray.CreateBuilder<Header>();

            foreach (var attribute in attributes)
            {
                foreach (var rawHeader in attribute.ConstructorArguments)
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    var headerStr = (string)rawHeader.Value!;
                    builder.Add(Header.Parse(headerStr));
                }
            }

            return builder;
        }

        private ReturnType ParseReturnType(IMethodSymbol method)
        {
            ReturnTypeKind returnKind;
            string?        innerTypeStr  = null;
            var            isAsync       = false;
            var            returnType    = method.ReturnType;
            var            returnTypeStr = returnType.ToDisplayString(s_fullTypeFormat);

            // Check for void return type
            if (method.ReturnsVoid)
            {
                returnKind = ReturnTypeKind.Void;
                goto end;
            }

            // Check for non-generic Task return type
            if (SymbolEqualityComparer.Default.Equals(returnType,
                                                      _knownSymbols.TaskVoid))
            {
                isAsync    = true;
                returnKind = ReturnTypeKind.Void;
                goto end;
            }

            // Check for Task<T> return type
            if (returnType is INamedTypeSymbol { TypeParameters.Length: 1 } namedType
             && SymbolEqualityComparer.Default.Equals(namedType.ConstructUnboundGenericType(), _knownSymbols.TaskT))
            {
                isAsync = true;
                // The rest needs the inner type
                returnType   = namedType.TypeParameters[0];
                innerTypeStr = returnType.ToDisplayString(s_fullTypeFormat);
            }

            // Check for HttpResponseMessage
            if (SymbolEqualityComparer.Default.Equals(returnType, _knownSymbols.HttpResponseMessage))
            {
                returnKind = ReturnTypeKind.HttpResponseMessage;
                goto end;
            }

            // Check for Stream
            if (SymbolEqualityComparer.Default.Equals(returnType, _knownSymbols.Stream))
            {
                returnKind = ReturnTypeKind.Stream;
                goto end;
            }

            // Check for String
            if (returnType.SpecialType == SpecialType.System_String)
            {
                returnKind = ReturnTypeKind.String;
                goto end;
            }

            // If everything else fails, it's a type that needs deserializing
            returnKind = ReturnTypeKind.Custom;

        end:
            return new ReturnType(returnKind, isAsync, returnTypeStr, innerTypeStr);
        }
    }
}
