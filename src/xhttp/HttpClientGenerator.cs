using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xhttp.Model;
using xhttp.Readers;

namespace xhttp;

[Generator(LanguageNames.CSharp)]
public class HttpClientGenerator : IIncrementalGenerator
{
    private static readonly Assembly s_assembly =
        typeof(HttpClientGenerator).Assembly;

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(postCtx =>
        {
            postCtx.AddEmbeddedAttributeDefinition();
            postCtx.AddSource("ParameterAttributes.cs",
                              GetEmbeddedText("ParameterAttributes.cs"));
            postCtx.AddSource("RequestAttributes.cs",
                              GetEmbeddedText("RequestAttributes.cs"));
        });

        GenerateRequests(MetadataNames.Head);
        GenerateRequests(MetadataNames.Get);
        GenerateRequests(MetadataNames.Post);
        GenerateRequests(MetadataNames.Put);
        GenerateRequests(MetadataNames.Patch);
        GenerateRequests(MetadataNames.Delete);
        GenerateRequests(MetadataNames.Options);
        GenerateRequests(MetadataNames.Request);
        return;

        void GenerateRequests(string metadataName)
        {
            var requests = context.SyntaxProvider.ForAttributeWithMetadataName(
                metadataName,
                (node, _) => node.IsKind(SyntaxKind.MethodDeclaration),
                RequestReader.ReadRequest);

        }
    }

    private static SourceText GetEmbeddedText(string name)
    {
        using var stream = s_assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return SourceText.From(reader, (int)stream.Length, Encoding.UTF8);
    }
}
