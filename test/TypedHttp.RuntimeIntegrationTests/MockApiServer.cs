using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using TypedHttp.RuntimeIntegrationTests.Contracts;

namespace TypedHttp.RuntimeIntegrationTests;

public sealed class MockApiServer : IAsyncDisposable
{
    private readonly WebApplication                            _application;
    private readonly ConcurrentDictionary<string, User>        _sampleUsers  = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, FeatureUser> _featureUsers = new(StringComparer.Ordinal);
    private          int                                       _nextSampleUserId;
    private          int                                       _nextFeatureUserId;

    private MockApiServer(WebApplication application, Uri baseAddress)
    {
        _application = application;
        BaseAddress  = baseAddress;
        MapRoutes(_application);
    }

    public Uri BaseAddress { get; private set; }

    public static async Task<MockApiServer> StartAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        var app    = builder.Build();
        var server = new MockApiServer(app, new Uri("http://127.0.0.1"));

        await app.StartAsync();
        var actualAddress = app.Urls.Single();
        server.BaseAddress = new Uri(actualAddress, UriKind.Absolute);
        return server;
    }

    public HttpClient CreateHttpClient(HttpMessageHandler? handler = null)
    {
        switch (handler)
        {
            case null: return new HttpClient { BaseAddress = BaseAddress };
            case DelegatingHandler { InnerHandler: null } delegatingHandler:
                delegatingHandler.InnerHandler = new SocketsHttpHandler();
                break;
        }

        return new HttpClient(handler) { BaseAddress = BaseAddress };
    }

    public ValueTask DisposeAsync()
    {
        return _application.DisposeAsync();
    }

    private void MapRoutes(WebApplication app)
    {
        app.MapGet(
            "/users",
            (HttpRequest request) =>
            {
                if (!HasBearerToken(request))
                {
                    return Results.Unauthorized();
                }

                return Results.Json(_sampleUsers.Values.OrderBy(user => user.Id));
            });

        app.MapGet(
            "/users/{id}",
            (HttpRequest request, string id) =>
            {
                if (!HasBearerToken(request))
                {
                    return Results.Unauthorized();
                }

                return _sampleUsers.TryGetValue(id, out var user)
                           ? Results.Json(user)
                           : Results.NotFound();
            });

        app.MapPost(
            "/users",
            async (HttpRequest request) =>
            {
                if (!HasBearerToken(request))
                {
                    return Results.Unauthorized();
                }

                var payload = await request.ReadFromJsonAsync<NewUser>();
                if (payload is null)
                {
                    return Results.BadRequest();
                }

                var id   = Interlocked.Increment(ref _nextSampleUserId).ToString();
                var user = new User(id, payload.Name, payload.Email, DateTimeOffset.UtcNow);
                _sampleUsers[id] = user;
                return Results.Json(user, statusCode: (int)HttpStatusCode.Created);
            });

        app.MapPut(
            "/users/{id}",
            async (HttpRequest request, string id) =>
            {
                if (!HasBearerToken(request))
                {
                    return Results.Unauthorized();
                }

                var payload = await request.ReadFromJsonAsync<NewUser>();
                if (payload is null || !_sampleUsers.TryGetValue(id, out var currentUser))
                {
                    return Results.NotFound();
                }

                _sampleUsers[id] = currentUser with { Name = payload.Name, Email = payload.Email };
                return Results.NoContent();
            });

        app.MapDelete(
            "/users/{id}",
            (HttpRequest request, string id) =>
            {
                if (!HasBearerToken(request))
                {
                    return Results.Unauthorized();
                }

                _sampleUsers.TryRemove(id, out _);
                return Results.NoContent();
            });

        app.MapGet(
            "/feature/users/{id}",
            (HttpRequest request, string id) =>
            {
                var include       = request.Query["include"].ToString();
                var traceHeader   = request.Headers["X-Trace"].ToString();
                var staticHeader  = request.Headers["X-Static"].ToString();
                var authorization = request.Headers.Authorization.ToString();

                var response = new GetUserResponse(id, include, traceHeader, staticHeader, authorization);
                return Results.Json(response);
            });

        app.MapPost(
            "/feature/users",
            async (HttpRequest request) =>
            {
                var payload = await request.ReadFromJsonAsync<CreateFeatureUser>();
                if (payload is null)
                {
                    return Results.BadRequest();
                }

                var id   = Interlocked.Increment(ref _nextFeatureUserId).ToString();
                var user = new FeatureUser(id, payload.Name, payload.Email);
                _featureUsers[id] = user;
                return Results.Json(user, statusCode: (int)HttpStatusCode.Created);
            });

        app.MapPut(
            "/feature/users/{id}",
            async (HttpRequest request, string id) =>
            {
                var payload = await request.ReadFromJsonAsync<CreateFeatureUser>();
                if (payload is null || !_featureUsers.TryGetValue(id, out var currentUser))
                {
                    return Results.NotFound();
                }

                _featureUsers[id] = currentUser with { Name = payload.Name, Email = payload.Email };
                return Results.NoContent();
            });

        app.MapPatch(
            "/feature/users/{id}",
            async (HttpRequest request, string id) =>
            {
                var payload = await request.ReadFromJsonAsync<PatchEmail>();
                if (payload is null || !_featureUsers.TryGetValue(id, out var currentUser))
                {
                    return Results.NotFound();
                }

                _featureUsers[id] = currentUser with { Email = payload.Email };
                return Results.Text($"patched:{id}:{payload.Email}", "text/plain");
            });

        app.MapDelete(
            "/feature/users/{id}",
            (string id) =>
            {
                _featureUsers.TryRemove(id, out _);
                return Results.NoContent();
            });

        app.MapMethods(
            "/feature/ping",
            [ "HEAD" ],
            async context =>
            {
                context.Response.Headers.Append("X-Ping", "pong");
                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.CompleteAsync();
            });

        app.MapMethods(
            "/feature/ping",
            [ "OPTIONS" ],
            async context =>
            {
                context.Response.Headers.Append("Allow", "HEAD, OPTIONS");
                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.CompleteAsync();
            });

        app.MapMethods(
            "/feature/echo",
            [ "TRACE" ],
            async (HttpRequest request) =>
            {
                using var reader  = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                var       payload = await reader.ReadToEndAsync();
                return Results.Text($"echo:{payload}", "text/plain");
            });

        app.MapGet(
            "/feature/download/{id}",
            (string id) =>
            {
                var data = Encoding.UTF8.GetBytes($"stream:{id}");
                return Results.Bytes(data, "application/octet-stream");
            });

        app.MapGet(
            "/feature/raw",
            () => Results.Json(new { status = "ok" }, statusCode: (int)HttpStatusCode.Accepted));

        app.MapGet(
            "/feature/value-text",
            () => Results.Text("value-task-text", "text/plain"));

        app.MapPost(
            "/feature/body-echo",
            async (HttpRequest request) =>
            {
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                return Results.Text(body, "text/plain");
            });

        app.MapGet(
            "/feature/search",
            (HttpRequest request) =>
            {
                var pageStr   = request.Query["page"].ToString();
                var filter    = request.Query.ContainsKey("filter") ? request.Query["filter"].ToString() : null;
                var methodTag = request.Headers["X-Method-Tag"].ToString();
                return Results.Json(new SearchResult(int.Parse(pageStr), filter, methodTag));
            });

        app.MapGet(
            "/feature/auth-echo",
            (HttpRequest request) => Results.Text(request.Headers.Authorization.ToString(), "text/plain"));

        app.MapGet(
            "/feature/response/{id}",
            (string id) =>
                id == "missing"
                    ? Results.NotFound()
                    : Results.Json(new FeatureUser(id, "Response User", "response@example.com")));
    }

    private static bool HasBearerToken(HttpRequest request)
    {
        return request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.Ordinal);
    }
}
