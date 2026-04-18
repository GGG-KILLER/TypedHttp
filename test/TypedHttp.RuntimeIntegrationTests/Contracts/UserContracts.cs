using System;

namespace TypedHttp.RuntimeIntegrationTests.Contracts;

public sealed record NewUser(string Name, string Email);

public sealed record User(string Id, string Name, string Email, DateTimeOffset CreatedAt);
