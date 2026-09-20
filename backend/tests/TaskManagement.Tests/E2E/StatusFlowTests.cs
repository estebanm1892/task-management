using System.Net;
using System.Net.Http.Json;

namespace TaskManagement.Tests.E2E;

[Collection("E2E")]
public sealed class StatusFlowTests(E2eApiFixture app)
{
    [Fact]
    public async Task Valid_status_transitions_are_visible_in_later_queries()
    {
        var user = await app.CreateUserAsync();
        var task = await app.CreateTaskAsync(user.Id, "Advance me");

        var inProgress = await app.Client.PutAsJsonAsync($"/api/tasks/{task.Id}/status", new { status = "InProgress" });
        var done = await app.Client.PutAsJsonAsync($"/api/tasks/{task.Id}/status", new { status = "Done" });
        var tasks = await app.Client.GetFromJsonAsync<TaskDto[]>($"/api/tasks?userId={user.Id}");

        Assert.Equal(HttpStatusCode.OK, inProgress.StatusCode);
        Assert.Equal(HttpStatusCode.OK, done.StatusCode);
        Assert.Equal("Done", Assert.Single(tasks!).Status);
    }

    [Fact]
    public async Task Forbidden_jump_preserves_pending_status()
    {
        var user = await app.CreateUserAsync();
        var task = await app.CreateTaskAsync(user.Id, "Do not skip");

        var rejected = await app.Client.PutAsJsonAsync($"/api/tasks/{task.Id}/status", new { status = "Done" });
        var tasks = await app.Client.GetFromJsonAsync<TaskDto[]>($"/api/tasks?userId={user.Id}");

        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        Assert.Equal("Pending", Assert.Single(tasks!).Status);
    }
}
