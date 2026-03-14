using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xhttp.Model;

namespace Xhttp;

public partial class HttpClientGenerator
{
    private sealed partial class Parser
    {
        public partial Client ParseClient(GeneratorAttributeSyntaxContext context)
        {
            var scopes =
                ParseContainingScopes((TypeDeclarationSyntax)context.TargetNode,
                                      out var interfaceModifiers,
                                      out var interfaceName);

            var headers = ParseHeaders(context.TargetSymbol);

            var requests   = ImmutableArray.CreateBuilder<Request>();
            var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;
            foreach (var method in typeSymbol.GetMembers()
                                             .OfType<IMethodSymbol>())
            {
                var request = TryParseRequest(method);
                if (request is not null) requests.Add(request);
            }

            return new Client(scopes,
                              interfaceModifiers,
                              interfaceName,
                              headers,
                              requests.DrainToImmutable().ByVal(),
                              _diagnostics.DrainToImmutable().ByVal());
        }

        /// <summary>
        /// Extracts headers from the provided symbol.
        /// </summary>
        private ImmutableByValArray<Header> ParseHeaders(ISymbol symbol)
        {
            var builder = ImmutableArray.CreateBuilder<Header>();

            foreach (var attribute in symbol.GetAttributes())
            {
                _cancellationToken.ThrowIfCancellationRequested();

                // Ignore attributes which aren't [Headers]
                if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass,
                                                           _knownSymbols.Headers))
                    continue;

                foreach (var header in attribute.ConstructorArguments)
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    // Split the "Name: Value" format
                    var str = (string)header.Value!;
                    builder.Add(Header.Parse(str));
                }
            }

            return builder.DrainToImmutable().ByVal();
        }

        /// <summary>
        /// Extracts the namespace, containing types and the client itself.
        /// </summary>
        /// <param name="clientDeclarationSyntax"></param>
        /// <param name="modifiers"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private ImmutableByValArray<string> ParseContainingScopes(
            TypeDeclarationSyntax clientDeclarationSyntax,
            out string            modifiers,
            out string            name)
        {
            var stringBuilder = new StringBuilder();
            var builder       = ImmutableArray.CreateBuilder<string>();

            // Client interface
            {
                modifiers = string.Join(" ",
                                        clientDeclarationSyntax.Modifiers
                                                               .Select(x => x.Text));

                var typeSymbol =
                    _semanticModel.GetDeclaredSymbol(clientDeclarationSyntax,
                                                     _cancellationToken);
                Debug.Assert(typeSymbol != null);

                name = typeSymbol!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            }

            // Containing types
            for (var currentType =
                     clientDeclarationSyntax.Parent as TypeDeclarationSyntax;
                 currentType != null;
                 currentType = currentType.Parent as TypeDeclarationSyntax)
            {
                // bool isPartialType = false;
                stringBuilder.Clear();

                foreach (SyntaxToken modifier in currentType.Modifiers)
                {
                    stringBuilder.Append(modifier.Text);
                    stringBuilder.Append(' ');
                    // isPartialType |=
                    // modifier.IsKind(SyntaxKind.PartialKeyword);
                }

                // TODO: Not partial diagnostic
                // if (!isPartialType)
                // {
                //     typeDeclarations = null;
                //     return false;
                // }

                stringBuilder.Append(currentType.Kind() switch
                                     {
                                         SyntaxKind.ClassDeclaration => "class",
                                         SyntaxKind.StructDeclaration =>
                                             "struct",
                                         _ => throw new Exception("Unreachable.")
                                     });
                stringBuilder.Append(' ');

                var typeSymbol =
                    _semanticModel.GetDeclaredSymbol(currentType,
                                                     _cancellationToken);
                Debug.Assert(typeSymbol != null);

                var typeName =
                    typeSymbol!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                stringBuilder.Append(typeName);

                builder.Add(stringBuilder.ToString());
            }

            // Namespace
            {
                var typeSymbol =
                    _semanticModel.GetDeclaredSymbol(clientDeclarationSyntax,
                                                     _cancellationToken);
                Debug.Assert(typeSymbol != null);

                if (typeSymbol?.ContainingNamespace is not null)
                {
                    stringBuilder.Append("namespace ");
                    stringBuilder.Append(typeSymbol.ContainingNamespace!.ToDisplayString(s_fullTypeFormat
                                            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle
                                                                         .Omitted)));
                    builder.Add(stringBuilder.ToString());
                }
            }

            builder.Reverse();

            return builder.DrainToImmutable().ByVal();
        }
    }
}
