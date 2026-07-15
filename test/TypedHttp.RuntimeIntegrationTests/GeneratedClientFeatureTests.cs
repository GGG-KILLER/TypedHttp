using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TypedHttp.RuntimeIntegrationTests.Contracts;
using Xunit;

namespace TypedHttp.RuntimeIntegrationTests;

public class GeneratedClientFeatureTests
{
    // ── Parameter binding ──────────────────────────────────────────────────

    [Fact]
    public async Task FeatureClient_BindsRouteQueryHeadersAuthorizeAndPropertyValues()
    {
        await using var server            = await MockApiServer.StartAsync();
        var             cancellationToken = TestContext.Current.CancellationToken;

        var            captureHandler = new PropertyCaptureHandler();
        using var      httpClient     = server.CreateHttpClient(captureHandler);
        IFeatureClient client         = new FeatureClient(httpClient);

        var response = await client.GetUser(
                           token: "token-123",
                           id: "42",
                           include: "all",
                           trace: "trace-abc",
                           requestProperty: "property-xyz",
                           cancellationToken: cancellationToken);

        Assert.Equal("42",                response.Id);
        Assert.Equal("all",               response.Include);
        Assert.Equal("trace-abc",         response.Trace);
        Assert.Equal("integration-suite", response.StaticHeader);
        Assert.Equal("Bearer token-123",  response.Authorization);
        Assert.Equal("property-xyz",      captureHandler.CapturedValue);
    }

    // ── HTTP methods: CRUD ─────────────────────────────────────────────────

    [Fact]
    public async Task FeatureClient_HttpMethods_PostPutPatchDeleteWork()
    {
        await using var server            = await MockApiServer.StartAsync();
        using var       httpClient        = server.CreateHttpClient();
        var             cancellationToken = TestContext.Current.CancellationToken;

        IFeatureClient client = new FeatureClient(httpClient);

        // POST
        var created = await client.CreateUser(new CreateFeatureUser("Bob", "bob@example.com"), cancellationToken);
        Assert.Equal("Bob",             created.Name);
        Assert.Equal("bob@example.com", created.Email);

        // PUT
        await client.UpdateUser(
            created.Id,
            new CreateFeatureUser("Bob Updated", "bob.updated@example.com"),
            cancellationToken);

        // PATCH
        var patchResult = await client.PatchUserEmail(
                              created.Id,
                              new PatchEmail("patched@example.com"),
                              cancellationToken);
        Assert.Equal($"patched:{created.Id}:patched@example.com", patchResult);

        // DELETE
        await client.DeleteUser(created.Id, cancellationToken);
    }

    // ── HTTP methods: HEAD / OPTIONS / TRACE ───────────────────────────────

    [Fact]
    public async Task FeatureClient_HeadRequest_ReturnsResponseMessage()
    {
        await using var server            = await MockApiServer.StartAsync();
        using var       httpClient        = server.CreateHttpClient();
        var             cancellationToken = TestContext.Current.CancellationToken;

        IFeatureClient client = new FeatureClient(httpClient);

        using var response = await client.HeadPing(cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("pong",            response.Headers.GetValues("X-Ping").Single());
    }

    [Fact]
    public async Task FeatureClient_OptionsRequest_ReturnsAllowHeader()
    {
        await using var server            = await MockApiServer.StartAsync();
        using var       httpClient        = server.CreateHttpClient();
        var             cancellationToken = TestContext.Current.CancellationToken;

        IFeatureClient client = new FeatureClient(httpClient);

        using var response = await client.OptionsPing(cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var hasAllow = response.Headers.TryGetValues("Allow", out var allowValues)
                    || response.Content.Headers.TryGetValues("Allow", out allowValues);
        Assert.True(hasAllow);

        var combined = string.Join(",", allowValues!);
        Assert.Contains("HEAD",    combined, StringComparison.Ordinal);
        Assert.Contains("OPTIONS", combined, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FeatureClient_TraceRequest_EchoesBody()
    {
        await using var server            = await MockApiServer.StartAsync();
        using var       httpClient        = server.CreateHttpClient();
        var             cancellationToken = TestContext.Current.CancellationToken;

        IFeatureClient client = new FeatureClient(httpClient);

        var result = await client.TraceEcho("trace-payload", cancellationToken);
        Assert.Equal("echo:trace-payload", result);
    }

    // ── Return types ───────────────────────────────────────────────────────

    [Fact]
    public async Task FeatureClient_StreamReturn_DownloadsContent()
    {
        await using var server            = await MockApiServer.StartAsync();
        using var       httpClient        = server.CreateHttpClient();
        var             cancellationToken = TestContext.Current.CancellationToken;

        IFeatureClient client = new FeatureClient(httpClient);

        await using var stream = await client.Download("file-1", cancellationToken);
        using var       reader = new StreamReader(stream);
        Assert.Equal("stream:file-1", await reader.ReadToEndAsync(cancellationToken));
    }

    [Fact]
    public async Task FeatureClient_ValueTaskHttpResponseMessage_ReturnsRawResponse()
    {
        await using var server            = await MockApiServer.StartAsync();
        using var       httpClient        = server.CreateHttpClient();
        var             cancellationToken = TestContext.Current.CancellationToken;

        IFeatureClient client = new FeatureClient(httpClient);

        using var response = await client.GetRaw(cancellationToken);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task FeatureClient_ValueTaskString_ReturnsTextContent()
    {
        await using var server            = await MockApiServer.StartAsync();
        using var       httpClient        = server.CreateHttpClient();
        var             cancellationToken = TestContext.Current.CancellationToken;

        IFeatureClient client = new FeatureClient(httpClient);

        var text = await client.GetValueText(cancellationToken);
        Assert.Equal("value-task-text", text);
    }

    // ── Body parameter types ───────────────────────────────────────────────

    [Fact]
    public async Task FeatureClient_BodyParameter_String_SentAsUtf8Bytes()
    {
        await using var server            = await MockApiServer.StartAsync();
        using var       httpClient        = server.CreateHttpClient();
        var             cancellationToken = TestContext.Current.CancellationToken;

        IFeatureClient client = new FeatureClient(httpClient);

        var result = await client.EchoString("hello-string", cancellationToken);
        Assert.Equal("hello-string", result);
    }

    [Fact]
    public async Task FeatureClient_BodyParameter_HttpContent_SentDirectly()
    {
        await using var server            = await MockApiServer.StartAsync();
        using var       httpClient        = server.CreateHttpClient();
        var             cancellationToken = TestContext.Current.CancellationToken;

        IFeatureClient client = new FeatureClient(httpClient);

        using var content = new StringContent("hello-content", Encoding.UTF8, "text/plain");
        var       result  = await client.EchoHttpContent(content, cancellationToken);
        Assert.Equal("hello-content", result);
    }

    [Fact]
    public async Task FeatureClient_BodyParameter_Stream_SentAsStreamContent()
    {
        await using var server            = await MockApiServer.StartAsync();
        using var       httpClient        = server.CreateHttpClient();
        var             cancellationToken = TestContext.Current.CancellationToken;

        IFeatureClient client = new FeatureClient(httpClient);

        using var stream = new MemoryStream("hello-stream"u8.ToArray());
        var       result = await client.EchoStream(stream, cancellationToken);
        Assert.Equal("hello-stream", result);
    }

    // ── Query parameters ───────────────────────────────────────────────────

    [Fact]
    public async Task FeatureClient_QueryParameter_NonStringType_CallsToString()
    {
        await using var server            = await MockApiServer.StartAsync();
        using var       httpClient        = server.CreateHttpClient();
        var             cancellationToken = TestContext.Current.CancellationToken;

        IFeatureClient client = new FeatureClient(httpClient);

        var result = await client.SearchFeature(page: 3, filter: "active", cancellationToken: cancellationToken);
        Assert.Equal(3,        result.Page);
        Assert.Equal("active", result.Filter);
    }

    [Fact]
    public async Task FeatureClient_QueryParameter_NullValue_IsOmittedFromUrl()
    {
        await using var server            = await MockApiServer.StartAsync();
        using var       httpClient        = server.CreateHttpClient();
        var             cancellationToken = TestContext.Current.CancellationToken;

        IFeatureClient client = new FeatureClient(httpClient);

        var result = await client.SearchFeature(page: 1, filter: null, cancellationToken: cancellationToken);
        Assert.Equal(1, result.Page);
        Assert.Null(result.Filter);
    }

    // ── Method-level [Headers] ─────────────────────────────────────────────

    [Fact]
    public async Task FeatureClient_MethodLevelHeaders_AreSentWithRequest()
    {
        await using var server            = await MockApiServer.StartAsync();
        using var       httpClient        = server.CreateHttpClient();
        var             cancellationToken = TestContext.Current.CancellationToken;

        IFeatureClient client = new FeatureClient(httpClient);

        var result = await client.SearchFeature(page: 1, filter: null, cancellationToken: cancellationToken);
        Assert.Equal("search-op", result.MethodTag);
    }

    // ── Custom [Authorize] scheme ──────────────────────────────────────────

    [Fact]
    public async Task FeatureClient_Authorize_CustomScheme_SendsCorrectPrefix()
    {
        await using var server            = await MockApiServer.StartAsync();
        using var       httpClient        = server.CreateHttpClient();
        var             cancellationToken = TestContext.Current.CancellationToken;

        IFeatureClient client = new FeatureClient(httpClient);

        var authHeader = await client.AuthEcho("my-token", cancellationToken);
        Assert.Equal("Token my-token", authHeader);
    }

    // ── Response<T> ─────────────────────────────────────────────────────────

    [Fact]
    public async Task FeatureClient_ResponseOfT_SuccessStatus_PopulatesBody()
    {
        await using var server            = await MockApiServer.StartAsync();
        using var       httpClient        = server.CreateHttpClient();
        var             cancellationToken = TestContext.Current.CancellationToken;

        IFeatureClient client = new FeatureClient(httpClient);

        var response = await client.GetUserResponse("42", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.IsSuccessStatusCode);
        Assert.NotNull(response.Body);
        Assert.Equal("42", response.Body.Id);
    }

    [Fact]
    public async Task FeatureClient_ResponseOfT_NotFoundStatus_DoesNotThrowAndBodyIsDefault()
    {
        await using var server            = await MockApiServer.StartAsync();
        using var       httpClient        = server.CreateHttpClient();
        var             cancellationToken = TestContext.Current.CancellationToken;

        IFeatureClient client = new FeatureClient(httpClient);

        // The whole point of Response<T> is to let callers inspect non-success responses
        // without an exception - unlike every other JSON return type, which calls
        // EnsureSuccessStatusCode() and throws.
        var response = await client.GetUserResponse("missing", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Null(response.Body);
    }
}
