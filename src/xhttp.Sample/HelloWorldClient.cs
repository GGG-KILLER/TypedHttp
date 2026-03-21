using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Xhttp.Sample;

[Client]
public interface ICrudClient
{
    [Get("users")]
    public Task<ImmutableArray<User>> GetAllUsers([Authorize] string token);

    [Get("users/{id}")]
    public Task<User> GetById([Authorize] string token, string id);

    [Post("users")]
    public Task<User> CreateUser([Authorize] string token, [Body] NewUser user);

    [Put("users/{id}")]
    public Task UpdateUser([Authorize] string token, string id, [Body] NewUser user);

    [Delete("users/{id}")]
    public Task DeleteUser([Authorize] string token, string id);
}
