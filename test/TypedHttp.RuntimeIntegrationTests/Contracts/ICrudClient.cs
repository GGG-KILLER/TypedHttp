using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace TypedHttp.RuntimeIntegrationTests.Contracts;

[Client]
public interface ICrudClient
{
    [Get("users")]
    Task<ImmutableArray<User>> GetAllUsers([Authorize] string token, CancellationToken cancellationToken = default);

    [Get("users/{id}")]
    Task<User> GetById([Authorize] string token, string id, CancellationToken cancellationToken = default);

    [Post("users")]
    Task<User> CreateUser([Authorize] string token, [Body] NewUser user, CancellationToken cancellationToken = default);

    [Put("users/{id}")]
    Task UpdateUser(
        [Authorize] string token,
        string             id,
        [Body] NewUser     user,
        CancellationToken  cancellationToken = default);

    [Delete("users/{id}")]
    Task DeleteUser([Authorize] string token, string id, CancellationToken cancellationToken = default);
}
