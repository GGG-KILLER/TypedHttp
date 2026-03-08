using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xhttp.Model;

namespace Xhttp;

[Generator(LanguageNames.CSharp)]
public partial class HttpClientGenerator : IIncrementalGenerator
{
    private static readonly Assembly s_assembly =
        typeof(HttpClientGenerator).Assembly;

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(postCtx =>
        {
            postCtx.AddEmbeddedAttributeDefinition();
            postCtx.AddSource("ClientAttributes.cs",
                              GetEmbeddedText("ClientAttributes.cs"));
            postCtx.AddSource("RequestAttributes.cs",
                              GetEmbeddedText("RequestAttributes.cs"));
            postCtx.AddSource("ParameterAttributes.cs",
                              GetEmbeddedText("ParameterAttributes.cs"));
        });

        var clients = context.SyntaxProvider.ForAttributeWithMetadataName(
            MetadataNames.Client,
            (node, _) => node.IsKind(SyntaxKind.ClassDeclaration)
                      || node.IsKind(SyntaxKind.StructDeclaration),
            (ctx, cancellationToken) =>
            {
                var parser = new Parser(ctx.SemanticModel, cancellationToken);
                return parser.ParseClient(ctx);
            });
    }

    private static SourceText GetEmbeddedText(string name)
    {
        using var stream = s_assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return SourceText.From(reader, (int)stream.Length, Encoding.UTF8);
    }

    private sealed partial class Parser(
        SemanticModel     semanticModel,
        CancellationToken cancellationToken = default)
    {
        // ReSharper disable once ReplaceWithPrimaryConstructorParameter
        private readonly SemanticModel _semanticModel = semanticModel;
        private readonly KnownSymbols  _knownSymbols  = new(semanticModel);

        private readonly ImmutableArray<Diagnostic>.Builder _diagnostics =
            ImmutableArray.CreateBuilder<Diagnostic>();

        // ReSharper disable once ReplaceWithPrimaryConstructorParameter
        private readonly CancellationToken _cancellationToken =
            cancellationToken;
    }
}
