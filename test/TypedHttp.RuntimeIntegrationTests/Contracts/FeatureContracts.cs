namespace TypedHttp.RuntimeIntegrationTests.Contracts;

public sealed record FeatureUser(string Id, string Name, string Email);

public sealed record CreateFeatureUser(string Name, string Email);

public sealed record PatchEmail(string Email);

public sealed record GetUserResponse(
    string Id,
    string Include,
    string Trace,
    string StaticHeader,
    string Authorization);

public sealed record SearchResult(int Page, string? Filter, string MethodTag);
