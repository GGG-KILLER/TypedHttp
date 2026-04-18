using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TypedHttp.RuntimeIntegrationTests.Contracts;

[Client]
[Headers("X-Static: integration-suite")]
public interface IFeatureClient
{
    [Get("feature/users/{id}")]
    Task<GetUserResponse> GetUser(
        [Authorize] string                                    token,
        string                                                id,
        [Query("include")]                             string include,
        [Header("X-Trace")]                            string trace,
        [Property(PropertyCaptureHandler.PropertyKey)] string requestProperty,
        CancellationToken                                     cancellationToken = default);

    [Post("feature/users")]
    Task<FeatureUser> CreateUser([Body] CreateFeatureUser user, CancellationToken cancellationToken = default);

    [Put("feature/users/{id}")]
    Task UpdateUser(string id, [Body] CreateFeatureUser user, CancellationToken cancellationToken = default);

    [Patch("feature/users/{id}")]
    Task<string> PatchUserEmail(string id, [Body] PatchEmail patch, CancellationToken cancellationToken = default);

    [Delete("feature/users/{id}")]
    Task DeleteUser(string id, CancellationToken cancellationToken = default);

    [Head("feature/ping")]
    Task<HttpResponseMessage> HeadPing(CancellationToken cancellationToken = default);

    [Options("feature/ping")]
    Task<HttpResponseMessage> OptionsPing(CancellationToken cancellationToken = default);

    [Request("TRACE", "feature/echo")]
    Task<string> TraceEcho([Body] string payload, CancellationToken cancellationToken = default);

    [Get("feature/download/{id}")]
    Task<Stream> Download(string id, CancellationToken cancellationToken = default);

    [Get("feature/raw")]
    ValueTask<HttpResponseMessage> GetRaw(CancellationToken cancellationToken = default);

    [Get("feature/value-text")]
    ValueTask<string> GetValueText(CancellationToken cancellationToken = default);

    // Body type variants
    [Post("feature/body-echo")]
    Task<string> EchoString([Body] string payload, CancellationToken cancellationToken = default);

    [Post("feature/body-echo")]
    Task<string> EchoHttpContent([Body] HttpContent content, CancellationToken cancellationToken = default);

    [Post("feature/body-echo")]
    Task<string> EchoStream([Body] Stream stream, CancellationToken cancellationToken = default);

    // Method-level [Headers], non-string query (int), nullable query omission
    [Get("feature/search")]
    [Headers("X-Method-Tag: search-op")]
    Task<SearchResult> SearchFeature(int page, string? filter, CancellationToken cancellationToken = default);

    // Custom [Authorize] scheme
    [Get("feature/auth-echo")]
    Task<string> AuthEcho([Authorize("Token")] string token, CancellationToken cancellationToken = default);
}
