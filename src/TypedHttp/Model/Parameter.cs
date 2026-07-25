namespace TypedHttp.Model;

internal readonly record struct Parameter(
    bool   IsNullable,
    string Type,
    string Name);
