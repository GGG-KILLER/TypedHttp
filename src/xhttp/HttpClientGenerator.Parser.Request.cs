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
                                   .ToLookup(attr => attr.AttributeClass!
                                                .MetadataName);

            var reqIds =
                attributes
                   .Where(x => MetadataNames.RequestIdentifiers
                                            .Contains(x.Key!))
                   .SelectMany(x => x)
                   .ToImmutableArray();
            if (reqIds.Length != 1) return null;
            var reqId = reqIds[0];

            string httpMethod, route;

            if (reqId.AttributeClass!.MetadataName == MetadataNames.Request)
            {
                httpMethod = (string)reqId.ConstructorArguments[0].Value!;
                route      = (string)reqId.ConstructorArguments[1].Value!;
            }
            else
            {
                httpMethod = reqId.AttributeClass.Name;
                httpMethod =
                    httpMethod[
                        ..httpMethod.IndexOf("Attribute",
                                             StringComparison.Ordinal)];
                route = (string)reqId.ConstructorArguments[0].Value!;
            }

            var headers =
                ParseRequestHeaders(attributes[MetadataNames.Headers]);

            return new Request(method.ToDisplayString(SymbolDisplayFormat
                                  .MinimallyQualifiedFormat),
                               httpMethod,
                               Template.Parse(route),
                               headers.DrainToImmutable().ByVal(),
                               TODO,
                               TODO);
        }

        private ImmutableArray<Header>.Builder ParseRequestHeaders(
            IEnumerable<AttributeData> attributes)
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

        private ReturnType ParseReturnType() { }
    }
}
