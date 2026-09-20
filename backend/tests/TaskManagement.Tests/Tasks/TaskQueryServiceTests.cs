using TaskManagement.Core.Tasks;
namespace TaskManagement.Tests.Tasks;
public sealed class TaskQueryServiceTests
{
    [Fact]
    public async Task List_validates_and_forwards_combined_filters()
    {
        var expected = new TaskItem(2, "Task", TaskState.Done, 7, DateTimeOffset.UtcNow, null);
        var repository = new FakeTaskRepository(expected);
        var service = new TaskService(repository);

        var result = await service.ListAsync(7, "Done", "High");
        Assert.Equal([expected], result);
        Assert.Equal(new TaskFilter(7, TaskState.Done, "High"), repository.LastFilter);
    }
    [Theory]
    [InlineData("Unknown", null)]
    [InlineData(null, "Urgent")]
    public async Task List_rejects_unknown_status_or_priority(string? status, string? priority)
    {
        await Assert.ThrowsAsync<TaskValidationException>(() =>
            new TaskService(new FakeTaskRepository()).ListAsync(null, status, priority));
    }
    [Fact]
    public async Task List_rejects_missing_user()
    {
        var repository = new FakeTaskRepository { UserExists = false };
        await Assert.ThrowsAsync<TaskResourceNotFoundException>(() =>
            new TaskService(repository).ListAsync(99, null, null));
    }
}
