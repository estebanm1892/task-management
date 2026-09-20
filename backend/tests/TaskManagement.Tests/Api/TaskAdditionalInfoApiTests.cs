using System.Net;
using System.Net.Http.Json;

namespace TaskManagement.Tests.Api;

public sealed class TaskAdditionalInfoApiTests
{
    [Fact]
    public async Task Patch_additional_info_updates_one_property_and_preserves_the_rest()
    {
        await using var application = new ApiTestApplication();
        var task = await application.Tasks.AddAsync(
            new(0, "Task", Core.Tasks.TaskState.Pending, 1, DateTimeOffset.UtcNow,
                "{\"priority\":\"Medium\",\"tags\":[\"api\"],\"owner\":\"ops\"}"), default);

        var response = await application.CreateClient().PatchAsJsonAsync(
            $"/api/tasks/{task.Id}/additional-info",
            new { property = "priority", value = "High" });
        var body = await response.Content.ReadFromJsonAsync<TaskResponseBody>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"priority\":\"High\",\"tags\":[\"api\"],\"owner\":\"ops\"}", body!.AdditionalInfo);
    }

    [Fact]
    public async Task Patch_additional_info_rejects_essential_properties_and_missing_tasks()
    {
        await using var application = new ApiTestApplication();
        var task = await application.Tasks.AddAsync(
            new(0, "Task", Core.Tasks.TaskState.Pending, 1, DateTimeOffset.UtcNow, "{\"priority\":\"Medium\"}"), default);
        var client = application.CreateClient();

        var essential = await client.PatchAsJsonAsync(
            $"/api/tasks/{task.Id}/additional-info",
            new { property = "Status", value = "Done" });
        var missing = await client.PatchAsJsonAsync(
            "/api/tasks/999/additional-info",
            new { property = "priority", value = "High" });

        Assert.Equal(HttpStatusCode.BadRequest, essential.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("{\"priority\":\"Medium\"}", application.Tasks.Snapshot.Single().AdditionalInfo);
    }

    [Fact]
    public async Task Patch_additional_info_rejects_invalid_priority_without_mutation()
    {
        await using var application = new ApiTestApplication();
        var task = await application.Tasks.AddAsync(
            new(0, "Task", Core.Tasks.TaskState.Pending, 1, DateTimeOffset.UtcNow, "{\"priority\":\"Medium\"}"), default);

        var response = await application.CreateClient().PatchAsJsonAsync(
            $"/api/tasks/{task.Id}/additional-info",
            new { property = "priority", value = "Urgent" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("{\"priority\":\"Medium\"}", application.Tasks.Snapshot.Single().AdditionalInfo);
    }

    [Fact]
    public async Task Patch_additional_info_rejects_missing_value()
    {
        await using var application = new ApiTestApplication();
        var task = await application.Tasks.AddAsync(
            new(0, "Task", Core.Tasks.TaskState.Pending, 1, DateTimeOffset.UtcNow, "{\"priority\":\"Medium\"}"), default);

        var response = await application.CreateClient().PatchAsJsonAsync(
            $"/api/tasks/{task.Id}/additional-info",
            new { property = "owner" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("{\"priority\":\"Medium\"}", application.Tasks.Snapshot.Single().AdditionalInfo);
    }

    private sealed record TaskResponseBody(
        int Id, string Title, string Status, int UserId, DateTimeOffset CreatedAt, string? AdditionalInfo);
}