namespace xhttp;

internal static class MetadataNames
{
    private const string Namespace = "Xhttp";

    // HTTP Methods
    public const string Head    = $"{Namespace}.HeadAttribute";
    public const string Get     = $"{Namespace}.GetAttribute";
    public const string Post    = $"{Namespace}.PostAttribute";
    public const string Put     = $"{Namespace}.PutAttribute";
    public const string Patch   = $"{Namespace}.PatchAttribute";
    public const string Delete  = $"{Namespace}.DeleteAttribute";
    public const string Options = $"{Namespace}.OptionsAttribute";
    public const string Request = $"{Namespace}.RequestAttribute";

    // Request attributes
    public const string Headers = $"{Namespace}.HeadersAttribute";

    // Parameter attributes
    public const string Alias     = $"{Namespace}.AliasAsAttribute";
    public const string Body      = $"{Namespace}.BodyAttribute";
    public const string Header    = $"{Namespace}.HeaderAttribute";
    public const string Property  = $"{Namespace}.PropertyAttribute";
    public const string Authorize = $"{Namespace}.AuthorizeAttribute";
}
