using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TypedHttp.Model;

namespace TypedHttp.Parsing;

internal sealed class ClientParser
{
    private readonly SemanticModel _semanticModel;
    private readonly KnownSymbols  _knownSymbols;
    private readonly ISymbol       _targetSymbol;
    private readonly SyntaxNode    _targetNode;

    private readonly ImmutableArray<DiagnosticInfo>.Builder _diagnostics =
        ImmutableArray.CreateBuilder<DiagnosticInfo>();

    public ClientParser(GeneratorAttributeSyntaxContext context)
    {
        _semanticModel = context.SemanticModel;
        _knownSymbols  = new KnownSymbols(_semanticModel);
        _targetSymbol  = context.TargetSymbol;
        _targetNode    = context.TargetNode;
    }

    public Client Parse(CancellationToken cancellationToken = default)
    {
        var scopes = ParseContainingScopes(
            (TypeDeclarationSyntax)_targetNode,
            out var interfaceModifiers,
            out var interfaceName);

        var headers = ParseHeaders(_targetSymbol);

        var requests      = ImmutableArray.CreateBuilder<Request>();
        var typeSymbol    = (INamedTypeSymbol)_targetSymbol;
        var requestParser = new RequestParser(_knownSymbols, _diagnostics);
        foreach (var method in typeSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            var request = requestParser.TryParse(method, cancellationToken);
            if (request is not null) requests.Add(request);
        }

        return new Client(
            Containers: scopes,
            Modifiers: interfaceModifiers,
            Interface: interfaceName,
            Headers: headers,
            Requests: requests.DrainToImmutable().ByVal(),
            Diagnostics: _diagnostics.DrainToImmutable().ByVal());
    }

    /// <summary>
    /// Extracts the namespace, containing types and the client itself.
    /// </summary>
    private ImmutableByValArray<string> ParseContainingScopes(
        TypeDeclarationSyntax clientDeclarationSyntax,
        out string            modifiers,
        out string            name,
        CancellationToken     cancellationToken = default)
    {
        var stringBuilder = new StringBuilder();
        var builder = ImmutableArray.CreateBuilder<string>();
        var typeSymbol = ModelExtensions.GetDeclaredSymbol(_semanticModel, clientDeclarationSyntax, cancellationToken);
        Debug.Assert(typeSymbol != null);

        // Client interface
        modifiers = string.Join(
            " ",
            clientDeclarationSyntax.Modifiers.Where(tk => !tk.IsKind(SyntaxKind.PartialKeyword)).Select(x => x.Text));
        name = typeSymbol!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        // Containing types
        for (var currentType = clientDeclarationSyntax.Parent as TypeDeclarationSyntax;
             currentType != null;
             currentType = currentType.Parent as TypeDeclarationSyntax)
        {
            bool isPartialType = false;
            stringBuilder.Clear();

            var containingTypeSymbol =
                ModelExtensions.GetDeclaredSymbol(_semanticModel, currentType, cancellationToken);
            Debug.Assert(containingTypeSymbol != null);

            foreach (SyntaxToken modifier in currentType.Modifiers)
            {
                stringBuilder.Append(modifier.Text);
                stringBuilder.Append(' ');
                isPartialType |= modifier.IsKind(SyntaxKind.PartialKeyword);
            }

            if (!isPartialType)
            {
                _diagnostics.Add(
                    DiagnosticInfo.Create(
                        Diagnostics.NonPartialParent,
                        currentType.Identifier.GetLocation(),
                        containingTypeSymbol!.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
            }

            stringBuilder.Append(
                currentType.Kind() switch
                {
                    SyntaxKind.ClassDeclaration        => "class",
                    SyntaxKind.StructDeclaration       => "struct",
                    SyntaxKind.RecordDeclaration       => "record",
                    SyntaxKind.RecordStructDeclaration => "record struct",
                    _                                  => throw new InvalidOperationException("Unreachable.")
                });
            stringBuilder.Append(' ');

            var typeName = containingTypeSymbol!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            stringBuilder.Append(typeName);

            builder.Add(stringBuilder.ToString());
        }

        // Namespace
        if (typeSymbol.ContainingNamespace is not null && !typeSymbol.ContainingNamespace.IsGlobalNamespace)
        {
            stringBuilder.Clear();
            stringBuilder.Append("namespace ");
            stringBuilder.Append(
                typeSymbol.ContainingNamespace.ToDisplayString(
                    SymbolDisplayFormats.FullTypeFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)));
            builder.Add(stringBuilder.ToString());
        }

        builder.Reverse();

        return builder.DrainToImmutable().ByVal();
    }

    /// <summary>
    /// Extracts headers from the provided symbol.
    /// </summary>
    private ImmutableByValArray<Header> ParseHeaders(ISymbol symbol, CancellationToken cancellationToken = default)
    {
        var builder = ImmutableArray.CreateBuilder<Header>();

        foreach (var attribute in symbol.GetAttributes())
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Ignore attributes which aren't [Headers]
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _knownSymbols.Headers)) continue;

            // Ignore empty [Headers] or ones with non-array arguments
            if (attribute.ConstructorArguments.Length  < 1
             || attribute.ConstructorArguments[0].Kind != TypedConstantKind.Array)
                continue;

            foreach (var header in attribute.ConstructorArguments[0].Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Split the "Name: Value" format
                var str = (string)header.Value!;

                var parsed = Header.Parse(str);
                if (parsed is { IsErr: true, Err.Value: var error })
                {
                    _diagnostics.Add(
                        Diagnostics.ForMalformedHeader(
                            attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                            symbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
                            error));
                }
                else
                {
                    builder.Add(parsed.Ok.Value);
                }
            }
        }

        return builder.DrainToImmutable().ByVal();
    }
}
