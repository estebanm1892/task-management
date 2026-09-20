using System.Net;
using System.Net.Http.Json;

namespace TaskManagement.Tests.E2E;

[Collection("E2E")]
public sealed class UserFlowTests(E2eApiFixture app)
{
    [Fact]
    public async Task Created_user_is_returned_by_a_later_request()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var created = await app.Client.PostAsJsonAsync("/api/users", new { name = "Ana", email });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var users = await app.Client.GetFromJsonAsync<UserDto[]>("/api/users");
        Assert.Contains(users!, user => user.Email == email && user.Name == "Ana");
    }

    [Fact]
    public async Task Equivalent_email_is_rejected_without_a_duplicate()
    {
        var email = $"unique-{Guid.NewGuid():N}@example.com";
        Assert.Equal(HttpStatusCode.Created,
            (await app.Client.PostAsJsonAsync("/api/users", new { name = "Luis", email })).StatusCode);

        var duplicate = await app.Client.PostAsJsonAsync("/api/users",
            new { name = "Other", email = $"  {email.ToUpperInvariant()}  " });
        var users = await app.Client.GetFromJsonAsync<UserDto[]>("/api/users");

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Single(users!, user => user.Email == email);
    }
}
