using System.Net;
using System.Net.Http.Json;
using TaskManagement.Core.Tasks;

namespace TaskManagement.Tests.Api;

public sealed class TaskStatusApiTests
{
    [Fact]
    public async Task Put_task_status_advances_allowed_transition_and_persists_result()
    {
        await using var application = new ApiTestApplication();
        var saved = await application.Tasks.AddAsync(new(0, "Task", TaskState.Pending, 1, DateTimeOffset.UtcNow, null), default);

        var response = await application.CreateClient().PutAsJsonAsync($"/api/tasks/{saved.Id}/status", new { status = "InProgress" });
        var body = await response.Content.ReadFromJsonAsync<TaskResponseBody>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("InProgress", body!.Status);
        Assert.Equal(TaskState.InProgress, application.Tasks.Snapshot.Single().Status);
    }

    [Fact]
    public async Task Put_task_status_returns_400_for_unknown_404_for_missing_and_409_for_forbidden_transition()
    {
        await using var application = new ApiTestApplication();
        var saved = await application.Tasks.AddAsync(new(0, "Task", TaskState.Pending, 1, DateTimeOffset.UtcNow, null), default);
        var client = application.CreateClient();

        var invalid = await client.PutAsJsonAsync($"/api/tasks/{saved.Id}/status", new { status = "Blocked" });
        var missing = await client.PutAsJsonAsync("/api/tasks/99/status", new { status = "InProgress" });
        var conflict = await client.PutAsJsonAsync($"/api/tasks/{saved.Id}/status", new { status = "Done" });

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(TaskState.Pending, application.Tasks.Snapshot.Single().Status);
    }

    private sealed record TaskResponseBody(int Id, string Title, string Status, int UserId, DateTimeOffset CreatedAt, string? AdditionalInfo);
}
