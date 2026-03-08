using Microsoft.CodeAnalysis;

namespace Xhttp.Model;

internal sealed class KnownSymbols(SemanticModel semanticModel)
{
    public INamedTypeSymbol String
        => field ??= GetTypeByMetadataName("System.String");

    public INamedTypeSymbol Stream
        => field ??= GetTypeByMetadataName("System.IO.Stream");

    public INamedTypeSymbol HttpContent
        => field ??= GetTypeByMetadataName("System.Net.Http.HttpContent");

    public INamedTypeSymbol HttpResponseMessage
        => field ??=
               GetTypeByMetadataName("System.Net.Http.HttpResponseMessage");

    public INamedTypeSymbol CancellationToken
        => field ??=
               GetTypeByMetadataName("System.Threading.CancellationToken");

    public INamedTypeSymbol TaskVoid
        => field ??= GetTypeByMetadataName("System.Threading.Tasks.Task");

    public INamedTypeSymbol TaskT
        => field ??= GetTypeByMetadataName("System.Threading.Tasks.Task`1");

    public INamedTypeSymbol Request
        => field ??= GetTypeByMetadataName(MetadataNames.Request);

    public INamedTypeSymbol Headers
        => field ??= GetTypeByMetadataName(MetadataNames.Headers);

    public INamedTypeSymbol Alias
        => field ??= GetTypeByMetadataName(MetadataNames.Alias);

    public INamedTypeSymbol Body
        => field ??= GetTypeByMetadataName(MetadataNames.Body);

    public INamedTypeSymbol Header
        => field ??= GetTypeByMetadataName(MetadataNames.Header);

    public INamedTypeSymbol Property
        => field ??= GetTypeByMetadataName(MetadataNames.Property);

    public INamedTypeSymbol Authorize
        => field ??= GetTypeByMetadataName(MetadataNames.Authorize);

    private INamedTypeSymbol GetTypeByMetadataName(string name)
        => semanticModel.Compilation.GetTypeByMetadataName(name)!
        ?? throw new InvalidOperationException(
               $"Unable to get INamedTypeSymbol for {name}");
}
