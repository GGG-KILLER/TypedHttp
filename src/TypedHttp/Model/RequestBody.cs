namespace TypedHttp.Model;

internal sealed record RequestBody(
    string Type,
    string Parameter,
    BodyKind Kind);

internal enum BodyKind
{
    HttpContent,
    Stream,
    String,
    Json
}
