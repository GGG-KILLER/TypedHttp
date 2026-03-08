using Microsoft.CodeAnalysis;

namespace Xhttp.Model;

internal sealed class KnownSymbols(SemanticModel semanticModel)
{
    public INamedTypeSymbol HttpContent
        => field ??= GetTypeByMetadataName("System.Net.Http.HttpContent");

    public INamedTypeSymbol Stream
        => field ??= GetTypeByMetadataName("System.IO.Stream");

    public INamedTypeSymbol String
        => field ??= GetTypeByMetadataName("System.String");

    public INamedTypeSymbol TaskVoid
        => field ??= GetTypeByMetadataName("System.Threading.Tasks.Task");

    public INamedTypeSymbol TaskT
        => field ??= GetTypeByMetadataName("System.Threading.Tasks.Task`1");

    public INamedTypeSymbol Headers
        => field ??= GetTypeByMetadataName(MetadataNames.Headers);

    private INamedTypeSymbol GetTypeByMetadataName(string name)
        => semanticModel.Compilation.GetTypeByMetadataName(name)!
        ?? throw new InvalidOperationException(
               $"Unable to get INamedTypeSymbol for {name}");
}
