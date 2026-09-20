using System.Net;
using System.Net.Http.Json;
using TaskManagement.Api.Tasks;

namespace TaskManagement.Tests.Api;

public sealed class TaskCreateApiTests
{
    [Fact]
    public void Create_task_request_maps_contract_fields_to_service_input()
    {
        var request = new CreateTaskRequest("  Build API  ", 3, "{\"priority\":\"High\"}");

        Assert.Equal(("  Build API  ", 3, "{\"priority\":\"High\"}"), request.ToServiceInput());
    }

    [Fact]
    public async Task Post_tasks_creates_pending_task_and_returns_public_payload()
    {
        await using var application = new ApiTestApplication();
        var client = application.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tasks", new { title = "  Build API  ", userId = 1, additionalInfo = "{\"priority\":\"High\"}" });
        var body = await response.Content.ReadFromJsonAsync<TaskResponseBody>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Build API", body!.Title);
        Assert.Equal("Pending", body.Status);
        Assert.Equal(1, body.UserId);
        Assert.Equal("{\"priority\":\"High\"}", body.AdditionalInfo);

        var locationResponse = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, locationResponse.StatusCode);
    }

    [Fact]
    public async Task Get_tasks_returns_404_for_missing_task()
    {
        await using var application = new ApiTestApplication();

        var response = await application.CreateClient().GetAsync("/api/tasks/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_tasks_returns_400_for_invalid_input_404_for_missing_user_and_503_for_persistence_failure()
    {
        await using var application = new ApiTestApplication();
        var client = application.CreateClient();

        var invalid = await client.PostAsJsonAsync("/api/tasks", new { title = "", userId = 1, additionalInfo = "not-json" });
        var notFound = await client.PostAsJsonAsync("/api/tasks", new { title = "Task", userId = 99 });
        application.Tasks.FailWrites = true;
        var unavailable = await client.PostAsJsonAsync("/api/tasks", new { title = "Task", userId = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
    }

    private sealed record TaskResponseBody(int Id, string Title, string Status, int UserId, DateTimeOffset CreatedAt, string? AdditionalInfo);
}
