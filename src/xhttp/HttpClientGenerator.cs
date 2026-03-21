using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Xhttp.IO;
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
        context.RegisterPostInitializationOutput(static postCtx =>
        {
            postCtx.AddEmbeddedAttributeDefinition();
            postCtx.AddSource("ClientAttributes.cs",
                              GetEmbeddedText("ClientAttributes.cs"));
            postCtx.AddSource("RequestAttributes.cs",
                              GetEmbeddedText("RequestAttributes.cs"));
            postCtx.AddSource("ParameterAttributes.cs",
                              GetEmbeddedText("ParameterAttributes.cs"));
        });

        var clients = context.SyntaxProvider.ForAttributeWithMetadataName(MetadataNames.Client,
                                                                          static (node, _)
                                                                              => node is InterfaceDeclarationSyntax,
                                                                          TransformNode);

        context.RegisterSourceOutput(clients, ProcessClient);
    }

    private static Client TransformNode(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
    {
        var parser = new Parser(ctx.SemanticModel,
                                cancellationToken);
        return parser.ParseClient(ctx);
    }

    private static void ProcessClient(SourceProductionContext ctx, Client client)
    {
        using var stringWriter       = new StringWriter();
        using var indentedTextWriter = new IndentedTextWriter(stringWriter);

        new ClientWriter(indentedTextWriter).WriteClient(client, ctx.CancellationToken);

        ctx.AddSource($"{client.Name.Substring(1)}.Generated.cs",
                      SourceText.From(stringWriter.ToString(), Encoding.UTF8));
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
        private static readonly SymbolDisplayFormat s_fullTypeFormat = new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
                                | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
                                | SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName
                                | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        // ReSharper disable once ReplaceWithPrimaryConstructorParameter
        private readonly SemanticModel _semanticModel = semanticModel;
        private readonly KnownSymbols  _knownSymbols  = new(semanticModel);

        private readonly ImmutableArray<Diagnostic>.Builder _diagnostics =
            ImmutableArray.CreateBuilder<Diagnostic>();

        // ReSharper disable once ReplaceWithPrimaryConstructorParameter
        private readonly CancellationToken _cancellationToken =
            cancellationToken;

        /// <summary>
        /// Parses a client from a <see cref="SyntaxValueProvider.ForAttributeWithMetadataName{T}"/>
        /// callback.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public partial Client ParseClient(GeneratorAttributeSyntaxContext context);
    }
}
