using System.Net;
using System.Net.Http.Json;
using TaskManagement.Core.Users;

namespace TaskManagement.Tests.Api;

public sealed class ApiErrorMiddlewareTests
{
    [Fact]
    public async Task Middleware_returns_problem_details_for_persistence_failures_without_internal_leakage()
    {
        await using var application = new ApiTestApplication();
        application.Users.FailReads = true;
        var client = application.CreateClient();

        var response = await client.GetAsync("/api/users");
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object?>>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("No fue posible completar la operación.", body!["title"]?.ToString());
        Assert.Contains("temporalmente", body["detail"]?.ToString());
        Assert.NotNull(body["traceId"]);
        Assert.DoesNotContain("database down", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Middleware_maps_duplicate_conflicts_to_consistent_problem_payload()
    {
        await using var application = new ApiTestApplication();
        await application.Users.AddAsync(new User(0, "Ana", "ana@example.com", "ana@example.com"), default);
        var client = application.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users", new { name = "Ana 2", email = " ANA@example.com " });
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object?>>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("No fue posible aplicar la solicitud.", body!["title"]?.ToString());
        Assert.Contains("mismo correo", body["detail"]?.ToString());
    }
}
