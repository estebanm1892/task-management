using System.Net;
using System.Net.Http.Json;

namespace TaskManagement.Tests.E2E;

[Collection("E2E")]
public sealed class TaskFlowTests(E2eApiFixture app)
{
    [Fact]
    public async Task Created_task_is_durable_and_filterable_by_user_status_and_priority()
    {
        var user = await app.CreateUserAsync();
        var response = await app.Client.PostAsJsonAsync("/api/tasks", new
        {
            title = "High priority delivery",
            userId = user.Id,
            additionalInfo = "{\"priority\":\"High\",\"tags\":[\"delivery\"]}"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var tasks = await app.Client.GetFromJsonAsync<TaskDto[]>(
            $"/api/tasks?userId={user.Id}&status=Pending&priority=High");
        var task = Assert.Single(tasks!);
        Assert.Equal(("High priority delivery", "Pending", user.Id), (task.Title, task.Status, task.UserId));
        Assert.Contains("\"priority\":\"High\"", task.AdditionalInfo);
    }

    [Fact]
    public async Task User_task_query_returns_newest_task_first()
    {
        var user = await app.CreateUserAsync();
        await app.CreateTaskAsync(user.Id, "First task");
        await app.CreateTaskAsync(user.Id, "Second task");

        var tasks = await app.Client.GetFromJsonAsync<TaskDto[]>(
            $"/api/tasks?userId={user.Id}&status=Pending");

        Assert.Equal(["Second task", "First task"], tasks!.Select(task => task.Title));
    }
}
