using System.Net;
using System.Net.Http.Json;
using TaskManagement.Core.Users;

namespace TaskManagement.Tests.Api;

public sealed class UsersApiTests
{
    [Fact]
    public async Task Post_users_creates_collaborator_with_location_and_public_payload()
    {
        await using var application = new ApiTestApplication();
        var client = application.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users", new { name = "  Ana  ", email = " ANA@example.com " });
        var body = await response.Content.ReadFromJsonAsync<UserResponseBody>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/users/1", response.Headers.Location?.AbsolutePath);
        Assert.Equal((1, "Ana", "ANA@example.com"), (body!.Id, body.Name, body.Email));

        var locationResponse = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, locationResponse.StatusCode);

        var locationBody = await locationResponse.Content.ReadFromJsonAsync<UserResponseBody>();
        Assert.Equal((body.Id, body.Name, body.Email), (locationBody!.Id, locationBody.Name, locationBody.Email));
    }

    [Fact]
    public async Task Get_user_returns_404_for_missing_and_400_for_invalid_id()
    {
        await using var application = new ApiTestApplication();
        var client = application.CreateClient();

        var missing = await client.GetAsync("/api/users/99");
        var invalid = await client.GetAsync("/api/users/0");

        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task Get_users_returns_persisted_collaborators_or_empty_collection()
    {
        await using var emptyApplication = new ApiTestApplication();
        Assert.Empty(await emptyApplication.CreateClient().GetFromJsonAsync<UserResponseBody[]>("/api/users") ?? []);

        await using var application = new ApiTestApplication();
        await application.Users.AddAsync(new User(0, "Ana", "ana@example.com", "ana@example.com"), default);

        var users = await application.CreateClient().GetFromJsonAsync<UserResponseBody[]>("/api/users");

        Assert.Equal(["ana@example.com"], users!.Select(user => user.Email));
    }

    [Fact]
    public async Task Post_users_returns_400_for_invalid_payload_409_for_duplicate_and_503_for_persistence_failure()
    {
        await using var application = new ApiTestApplication();
        var client = application.CreateClient();

        var invalid = await client.PostAsJsonAsync("/api/users", new { name = "", email = "bad" });
        await application.Users.AddAsync(new User(0, "Ana", "ana@example.com", "ana@example.com"), default);
        var duplicate = await client.PostAsJsonAsync("/api/users", new { name = "Ana", email = "ANA@example.com" });
        application.Users.FailReads = true;
        var unavailable = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
    }

    private sealed record UserResponseBody(int Id, string Name, string Email);
}
