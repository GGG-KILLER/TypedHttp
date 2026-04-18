using System.Linq;
using System.Threading.Tasks;
using TypedHttp.RuntimeIntegrationTests.Contracts;
using Xunit;

namespace TypedHttp.RuntimeIntegrationTests;

public class CrudFlowIntegrationTests
{
    [Fact]
    public async Task CrudClient_PerformsCrudLifecycleAgainstMockServer()
    {
        await using var server            = await MockApiServer.StartAsync();
        using var       httpClient        = server.CreateHttpClient();
        var             cancellationToken = TestContext.Current.CancellationToken;

        ICrudClient client = new CrudClient(httpClient);

        var created = await client.CreateUser(
                          "integration-token",
                          new NewUser("Alice", "alice@example.com"),
                          cancellationToken);
        Assert.Equal("Alice", created.Name);

        var fetched = await client.GetById("integration-token", created.Id, cancellationToken);
        Assert.Equal(created.Id, fetched.Id);

        await client.UpdateUser(
            "integration-token",
            created.Id,
            new NewUser("Alice Updated", "alice.updated@example.com"),
            cancellationToken);

        var updated = await client.GetById("integration-token", created.Id, cancellationToken);
        Assert.Equal("Alice Updated",             updated.Name);
        Assert.Equal("alice.updated@example.com", updated.Email);

        var users = await client.GetAllUsers("integration-token", cancellationToken);
        Assert.True(users.Any(user => user.Id == created.Id));

        await client.DeleteUser("integration-token", created.Id, cancellationToken);

        var afterDelete = await client.GetAllUsers("integration-token", cancellationToken);
        Assert.False(afterDelete.Any(user => user.Id == created.Id));
    }
}
