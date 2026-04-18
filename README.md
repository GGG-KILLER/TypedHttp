# xHTTP

A strongly typed HTTP client source generator aiming for simplicity, zero extra runtime cost and ease of use. Similar to [Refit](https://github.com/reactiveui/refit), but implemented entirely with Roslyn source generators for maximal performance and full Ahead-of-Time (AOT) compilation compatibility.

## Installation

<!-- x-release-please-start-version -->
```bash
dotnet add package xhttp --version 0.0.0
```
<!-- x-release-please-end -->

Or add the package reference to your `.csproj`:

<!-- x-release-please-start-version -->
```xml
<PackageReference Include="xhttp" Version="0.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
```
<!-- x-release-please-end -->

> **Requirements**: xhttp is a C# source generator. It requires a compiler that supports source generators — **Visual Studio 2022 17.0+**, **.NET SDK 6.0+**, or **Rider 2021.3+**. The consuming project itself can target any framework (.NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+, etc.).

## Quick Start

Define an interface with the `[Client]` attribute and annotate each method with a request attribute:

```csharp
using Xhttp;

[Client]
public interface ICrudClient
{
    [Get("users")]
    Task<ImmutableArray<User>> GetAllUsers(
        [Authorize] string token,
        CancellationToken cancellationToken = default);

    [Get("users/{id}")]
    Task<User> GetById(
        [Authorize] string token,
        string id,
        CancellationToken cancellationToken = default);

    [Post("users")]
    Task<User> CreateUser(
        [Authorize] string token,
        [Body] NewUser user,
        CancellationToken cancellationToken = default);

    [Put("users/{id}")]
    Task UpdateUser(
        [Authorize] string token,
        string id,
        [Body] NewUser user,
        CancellationToken cancellationToken = default);

    [Delete("users/{id}")]
    Task DeleteUser(
        [Authorize] string token,
        string id,
        CancellationToken cancellationToken = default);
}
```

xhttp generates a `CrudClient` class (the interface name minus the `I` prefix) that implements your interface. Use it like:

```csharp
var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com/") };

// Basic usage
ICrudClient client = new CrudClient(httpClient);

// With System.Text.Json options
ICrudClient client = new CrudClient(httpClient, myJsonSerializerOptions);

// With a JsonSerializerContext (AOT-friendly)
ICrudClient client = new CrudClient(httpClient, MyJsonContext.Default);
```

## Attributes

### Request Attributes

Every method must have exactly one request attribute:

| Attribute                      | Description          |
|--------------------------------|----------------------|
| `[Get("route")]`               | HTTP GET request     |
| `[Post("route")]`              | HTTP POST request    |
| `[Put("route")]`               | HTTP PUT request     |
| `[Delete("route")]`            | HTTP DELETE request  |
| `[Patch("route")]`             | HTTP PATCH request   |
| `[Head("route")]`              | HTTP HEAD request    |
| `[Options("route")]`           | HTTP OPTIONS request |
| `[Request("METHOD", "route")]` | Custom HTTP method   |

Route templates support placeholders like `{id}` that map to method parameters by name.

### Parameter Attributes

| Attribute               | Description                                                                                                          |
|-------------------------|----------------------------------------------------------------------------------------------------------------------|
| `[Body]`                | Send the parameter as the request body (JSON by default; strings as plain text; `Stream` and `HttpContent` directly) |
| `[Query("name")]`       | Override the query string parameter name (equivalent to Refit's `[AliasAs]`)                                         |
| `[Header("Name")]`      | Send the parameter as an HTTP header                                                                                 |
| `[Authorize("scheme")]` | Add an `Authorization` header (default scheme: `Bearer`)                                                             |
| `[Property("key")]`     | Store the parameter in `HttpRequestMessage.Properties` for use in custom `DelegatingHandler`s                        |

Parameters not decorated with any attribute are automatically mapped as:
- **Route parameters** if their name matches a `{placeholder}` in the route template
- **Query parameters** otherwise

### Interface/Method Attributes

| Attribute                       | Description                                                                         |
|---------------------------------|-------------------------------------------------------------------------------------|
| `[Client]`                      | Marks an interface for client generation                                            |
| `[Headers("Name: Value", ...)]` | Add static headers to all requests (on interface) or a specific request (on method) |

## Return Types

Methods must return one of the following async types:

| Return Type                                                    | Behavior                                                          |
|----------------------------------------------------------------|-------------------------------------------------------------------|
| `Task` / `ValueTask`                                           | Fire-and-forget (calls `EnsureSuccessStatusCode()`)               |
| `Task<HttpResponseMessage>` / `ValueTask<HttpResponseMessage>` | Returns the raw response (no `EnsureSuccessStatusCode()`)         |
| `Task<string>` / `ValueTask<string>`                           | Returns the response body as a string                             |
| `Task<Stream>` / `ValueTask<Stream>`                           | Returns the response body as a stream                             |
| `Task<T>` / `ValueTask<T>`                                     | Deserializes the response body from JSON using `System.Text.Json` |

## JSON Serialization

xhttp uses `System.Text.Json` exclusively. The generated client provides three constructors:

1. **`new CrudClient(HttpClient)`** — uses default `JsonSerializerOptions`
2. **`new CrudClient(HttpClient, JsonSerializerOptions)`** — uses custom options
3. **`new CrudClient(HttpClient, JsonSerializerContext)`** — uses a source-generated context (AOT-compatible)

## Known Limitations

> These are tracked and will be addressed in future releases (primarily via diagnostics in v0.2).

- **Methods must return `Task`, `Task<T>`, `ValueTask`, or `ValueTask<T>`.** Returning a bare type (e.g., `User` instead of `Task<User>`) will produce invalid generated code. Always use an async return type.
- **Route template placeholders must exactly match parameter names.** A typo in a placeholder like `{usrId}` when the parameter is named `userId` will produce a compile error on the generated code without a clear indication of the source. Double-check your route templates.
- **Only one `[Body]` parameter per method.** If multiple parameters are marked with `[Body]`, only the first one is used. The others are silently ignored.
- **Do not combine conflicting parameter attributes.** Using mutually exclusive attributes on the same parameter (e.g., `[Authorize]` + `[Body]`, or `[Header]` + `[Body]`) produces inconsistent generated code. Use only one behavioral attribute per parameter.
- **Methods without a request attribute are silently skipped.** If a method in a `[Client]` interface is missing a request attribute (e.g., `[Get]`, `[Post]`), no code is generated for it, resulting in a compile error on the generated class. Every method must have exactly one request attribute.
- **Containing types must be `partial`.** If the `[Client]` interface is nested inside a class or struct, that outer type must be declared `partial`. There is currently no diagnostic for this.

## Migrating from Refit

| Refit                   | xhttp                   | Notes                                                         |
|-------------------------|-------------------------|---------------------------------------------------------------|
| `[Get]`, `[Post]`, etc. | `[Get]`, `[Post]`, etc. | Same names                                                    |
| `[Body]`                | `[Body]`                | Same                                                          |
| `[AliasAs("name")]`     | `[Query("name")]`       | Renamed for clarity                                           |
| `[Header("Name")]`      | `[Header("Name")]`      | Same                                                          |
| `[HeaderCollection]`    | Not yet supported       |                                                               |
| `[Authorize]`           | `[Authorize]`           | Same                                                          |
| `[Property("key")]`     | `[Property("key")]`     | Same                                                          |
| `IApiResponse<T>`       | Not supported           | Use `Task<HttpResponseMessage>` for raw access                |
| Newtonsoft.Json         | Not supported           | System.Text.Json only (Newtonsoft.Json support is a non-goal) |

## TODO

- [x] Fully working source generation.
- [ ] Feature detection by TFM and conditional code generation.
- [ ] Refit [attribute](https://github.com/reactiveui/refit/blob/main/Refit/Attributes.cs) parity (with adaptations for cases where attributes can be joined or renamed for better clarity).
- [ ] Class/struct support.
- [ ] Support for `{**part}` in routes.
- [ ] Diagnostics for common mistakes and limitations.
- [ ] Better support for source-generated JsonSerializerContexts.

## Non-goals

- Support for Newtonsoft.Json or other serializers.
- Customizable route parameters, query parameters and header serialization.

## License

[MIT](LICENSE)
