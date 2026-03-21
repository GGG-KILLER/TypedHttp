using System;

namespace Xhttp.Sample;

public sealed record User(string Id, string Name, string Email, DateTimeOffset CreatedAt);