using System;

namespace TypedHttp.Sample;

public sealed record User(string Id, string Name, string Email, DateTimeOffset CreatedAt);