using System.Collections.Immutable;

namespace Xhttp;

internal static class MetadataNames
{
    private const string Namespace = "Xhttp";

    // Client attributes
    public const string Client = $"{Namespace}.ClientAttribute";

    // Request attributes
    public static readonly ImmutableArray<string> RequestIdentifiers =
    [
        Head, Get, Post, Put, Patch, Delete, Options, Request
    ];

    public const string Head    = $"{Namespace}.HeadAttribute";
    public const string Get     = $"{Namespace}.GetAttribute";
    public const string Post    = $"{Namespace}.PostAttribute";
    public const string Put     = $"{Namespace}.PutAttribute";
    public const string Patch   = $"{Namespace}.PatchAttribute";
    public const string Delete  = $"{Namespace}.DeleteAttribute";
    public const string Options = $"{Namespace}.OptionsAttribute";
    public const string Request = $"{Namespace}.RequestAttribute";
    public const string Headers = $"{Namespace}.HeadersAttribute";

    // Parameter attributes
    public const string Alias     = $"{Namespace}.AliasAsAttribute";
    public const string Body      = $"{Namespace}.BodyAttribute";
    public const string Header    = $"{Namespace}.HeaderAttribute";
    public const string Property  = $"{Namespace}.PropertyAttribute";
    public const string Authorize = $"{Namespace}.AuthorizeAttribute";
}
