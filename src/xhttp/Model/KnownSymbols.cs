using System.Collections.Immutable;
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

    public INamedTypeSymbol Head
        => field ??= GetTypeByMetadataName("Xhttp.HeadAttribute");

    public INamedTypeSymbol Get
        => field ??= GetTypeByMetadataName("Xhttp.GetAttribute");

    public INamedTypeSymbol Post
        => field ??= GetTypeByMetadataName("Xhttp.PostAttribute");

    public INamedTypeSymbol Put
        => field ??= GetTypeByMetadataName("Xhttp.PutAttribute");

    public INamedTypeSymbol Patch
        => field ??= GetTypeByMetadataName("Xhttp.PatchAttribute");

    public INamedTypeSymbol Delete
        => field ??= GetTypeByMetadataName("Xhttp.DeleteAttribute");

    public INamedTypeSymbol Options
        => field ??= GetTypeByMetadataName("Xhttp.OptionsAttribute");

    public INamedTypeSymbol Request
        => field ??= GetTypeByMetadataName("Xhttp.RequestAttribute");

    public ImmutableHashSet<INamedTypeSymbol> RequestMarkers
        => field ??=
               ImmutableHashSet.CreateRange<INamedTypeSymbol>(
                   SymbolEqualityComparer.Default,
                   [ Head, Get, Post, Put, Patch, Delete, Options, Request, ]);

    public INamedTypeSymbol Headers
        => field ??= GetTypeByMetadataName("Xhttp.HeadersAttribute");

    public INamedTypeSymbol Alias
        => field ??= GetTypeByMetadataName("Xhttp.AliasAsAttribute");

    public INamedTypeSymbol Body
        => field ??= GetTypeByMetadataName("Xhttp.BodyAttribute");

    public INamedTypeSymbol Header
        => field ??= GetTypeByMetadataName("Xhttp.HeaderAttribute");

    public INamedTypeSymbol Property
        => field ??= GetTypeByMetadataName("Xhttp.PropertyAttribute");

    public INamedTypeSymbol Authorize
        => field ??= GetTypeByMetadataName("Xhttp.AuthorizeAttribute");

    private INamedTypeSymbol GetTypeByMetadataName(string name)
        => semanticModel.Compilation.GetTypeByMetadataName(name)!
        ?? throw new InvalidOperationException(
               $"Unable to get INamedTypeSymbol for {name}");
}
