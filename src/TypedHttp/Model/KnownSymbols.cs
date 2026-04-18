using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace TypedHttp.Model;

internal sealed class KnownSymbols(SemanticModel semanticModel)
{
    public INamedTypeSymbol String => field ??= GetTypeByMetadataName(typeof(string).FullName!);

    public INamedTypeSymbol Stream => field ??= GetTypeByMetadataName(typeof(Stream).FullName!);

    public INamedTypeSymbol HttpContent => field ??= GetTypeByMetadataName(typeof(HttpContent).FullName!);

    public INamedTypeSymbol HttpResponseMessage
        => field ??=
               GetTypeByMetadataName(typeof(HttpResponseMessage).FullName!);

    public INamedTypeSymbol CancellationToken
        => field ??=
               GetTypeByMetadataName(typeof(CancellationToken).FullName!);

    public INamedTypeSymbol Task => field ??= GetTypeByMetadataName(typeof(Task).FullName!);

    public INamedTypeSymbol TaskOfT => field ??= GetTypeByMetadataName(typeof(Task<>).FullName!);

    public INamedTypeSymbol ValueTask => field ??= GetTypeByMetadataName(typeof(ValueTask).FullName!);

    public INamedTypeSymbol ValueTaskOfT => field ??= GetTypeByMetadataName(typeof(ValueTask<>).FullName!);

    public INamedTypeSymbol Head => field ??= GetTypeByMetadataName("TypedHttp.HeadAttribute");

    public INamedTypeSymbol Get => field ??= GetTypeByMetadataName("TypedHttp.GetAttribute");

    public INamedTypeSymbol Post => field ??= GetTypeByMetadataName("TypedHttp.PostAttribute");

    public INamedTypeSymbol Put => field ??= GetTypeByMetadataName("TypedHttp.PutAttribute");

    public INamedTypeSymbol Patch => field ??= GetTypeByMetadataName("TypedHttp.PatchAttribute");

    public INamedTypeSymbol Delete => field ??= GetTypeByMetadataName("TypedHttp.DeleteAttribute");

    public INamedTypeSymbol Options => field ??= GetTypeByMetadataName("TypedHttp.OptionsAttribute");

    public INamedTypeSymbol Request => field ??= GetTypeByMetadataName("TypedHttp.RequestAttribute");

    public ImmutableHashSet<INamedTypeSymbol> RequestMarkers
        => field ??=
               ImmutableHashSet.CreateRange<INamedTypeSymbol>(SymbolEqualityComparer.Default,
                                                              [
                                                                  Head, Get, Post, Put, Patch, Delete, Options, Request,
                                                              ]);

    public INamedTypeSymbol Headers => field ??= GetTypeByMetadataName("TypedHttp.HeadersAttribute");

    public INamedTypeSymbol Query => field ??= GetTypeByMetadataName("TypedHttp.QueryAttribute");

    public INamedTypeSymbol Body => field ??= GetTypeByMetadataName("TypedHttp.BodyAttribute");

    public INamedTypeSymbol Header => field ??= GetTypeByMetadataName("TypedHttp.HeaderAttribute");

    public INamedTypeSymbol Property => field ??= GetTypeByMetadataName("TypedHttp.PropertyAttribute");

    public INamedTypeSymbol Authorize => field ??= GetTypeByMetadataName("TypedHttp.AuthorizeAttribute");

    private INamedTypeSymbol GetTypeByMetadataName(string name)
        => semanticModel.Compilation.GetTypeByMetadataName(name)
        ?? throw new InvalidOperationException($"Unable to get INamedTypeSymbol for {name}");
}
