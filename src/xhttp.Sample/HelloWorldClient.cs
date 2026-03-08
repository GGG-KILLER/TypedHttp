using System.Text.Json;
using System.Threading.Tasks;

namespace Xhttp.Sample;

[Client]
public interface IHelloWorldClient
{
    [Get("hello-world")]
    public Task<string> Hello([Authorize] string token, string name);
}
