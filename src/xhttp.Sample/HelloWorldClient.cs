using System.Threading.Tasks;

namespace Xhttp.Sample;

public partial class HelloWorldClient
{
    [Get("/hello-world")]
    public partial Task<string> Hello([Authorize] string token, string name);
}
