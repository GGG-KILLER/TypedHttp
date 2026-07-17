using System.CodeDom.Compiler;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using TypedHttp.Emit;
using TypedHttp.Model;
using TypedHttp.Parsing;

namespace TypedHttp;

[Generator(LanguageNames.CSharp)]
public class HttpClientGenerator : IIncrementalGenerator
{
    private static readonly Assembly s_assembly =
        typeof(HttpClientGenerator).Assembly;

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static postCtx =>
        {
            postCtx.AddEmbeddedAttributeDefinition();
            postCtx.AddSource(
                "ClientAttributes.cs",
                GetEmbeddedText("ClientAttributes.cs"));
            postCtx.AddSource(
                "RequestAttributes.cs",
                GetEmbeddedText("RequestAttributes.cs"));
            postCtx.AddSource(
                "ParameterAttributes.cs",
                GetEmbeddedText("ParameterAttributes.cs"));
        });

        var clients = context.SyntaxProvider.ForAttributeWithMetadataName(
            MetadataNames.Client,
            static (node, _)
                => node is InterfaceDeclarationSyntax,
            TransformNode);

        context.RegisterSourceOutput(clients, ProcessClient);
    }

    private static Client TransformNode(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
    {
        var parser = new ClientParser(ctx);
        return parser.Parse(cancellationToken);
    }

    private static void ProcessClient(SourceProductionContext ctx, Client client)
    {
        if (!client.Diagnostics.Array.IsDefaultOrEmpty)
        {
            foreach (var diagnostic in client.Diagnostics)
            {
                ctx.ReportDiagnostic(diagnostic.CreateDiagnostic());
            }
            return;
        }

        using var stringWriter       = new StringWriter();
        using var indentedTextWriter = new IndentedTextWriter(stringWriter);

        new ClientWriter(indentedTextWriter).WriteClient(client, ctx.CancellationToken);

        ctx.AddSource(
            $"{client.Name}.Generated.cs",
            SourceText.From(stringWriter.ToString(), Encoding.UTF8));
    }

    private static SourceText GetEmbeddedText(string name)
    {
        using var stream = s_assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return SourceText.From(reader, (int)stream.Length, Encoding.UTF8);
    }
}
