using System.Net;
using System.Net.Http.Json;
using TaskManagement.Core.Tasks;

namespace TaskManagement.Tests.Api;

public sealed class TaskQueryApiTests
{
    [Fact]
    public async Task Get_tasks_lists_filters_and_orders_by_newest_first()
    {
        await using var application = new ApiTestApplication();
        await application.Tasks.AddAsync(new(0, "Old", TaskState.Pending, 1, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), "{\"priority\":\"High\"}"), default);
        await application.Tasks.AddAsync(new(0, "Done", TaskState.Done, 1, new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero), null), default);
        await application.Tasks.AddAsync(new(0, "Newest", TaskState.Pending, 1, new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero), "{\"priority\":\"High\"}"), default);

        var tasks = await application.CreateClient().GetFromJsonAsync<TaskResponseBody[]>("/api/tasks?userId=1&status=Pending&priority=High");

        Assert.Equal(["Newest", "Old"], tasks!.Select(task => task.Title));
        Assert.All(tasks!, task => Assert.Equal("Pending", task.Status));
    }

    [Fact]
    public async Task Get_tasks_returns_400_for_invalid_filters_and_404_for_missing_user()
    {
        await using var application = new ApiTestApplication();
        var client = application.CreateClient();

        var invalidStatus = await client.GetAsync("/api/tasks?status=Blocked");
        var invalidPriority = await client.GetAsync("/api/tasks?priority=Urgent");
        var notFound = await client.GetAsync("/api/tasks?userId=99");

        Assert.Equal(HttpStatusCode.BadRequest, invalidStatus.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidPriority.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }

    private sealed record TaskResponseBody(int Id, string Title, string Status, int UserId, DateTimeOffset CreatedAt, string? AdditionalInfo);
}
