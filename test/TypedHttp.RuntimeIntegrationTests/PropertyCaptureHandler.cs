using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TypedHttp.RuntimeIntegrationTests;

public sealed class PropertyCaptureHandler : DelegatingHandler
{
    public const string PropertyKey = "request-id";

    public string? CapturedValue { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
#pragma warning disable CS0618
        if (request.Properties.TryGetValue(PropertyKey, out var value) && value is string stringValue)
        {
            CapturedValue = stringValue;
        }
#pragma warning restore CS0618

        return base.SendAsync(request, cancellationToken);
    }
}
