using TaskManagement.Core.Tasks;

namespace TaskManagement.Tests.Tasks;

public sealed class TaskServiceTests
{
    [Fact]
    public async Task Create_sets_pending_assignment_time_and_valid_json()
    {
        var repository = new FakeTaskRepository();
        var time = new DateTimeOffset(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);
        var service = new TaskService(repository, new FixedTimeProvider(time));

        var task = await service.CreateAsync("  Ship feature  ", 1, "{\"priority\":\"High\"}");

        Assert.Equal(("Ship feature", 1, TaskState.Pending, time),
            (task.Title, task.UserId, task.Status, task.CreatedAt));
        Assert.Equal("{\"priority\":\"High\"}", task.AdditionalInfo);
        Assert.Equal([task], repository.Tasks);
    }

    [Theory]
    [InlineData("", 1, null)]
    [InlineData("Task", 0, null)]
    [InlineData("Task", 1, "not-json")]
    [InlineData("Task", 1, "{\"priority\":\"Urgent\"}")]
    public async Task Create_rejects_invalid_input_without_partial_write(
        string title, int userId, string? additionalInfo)
    {
        var repository = new FakeTaskRepository();
        var service = new TaskService(repository);

        await Assert.ThrowsAsync<TaskValidationException>(() =>
            service.CreateAsync(title, userId, additionalInfo));
        Assert.Empty(repository.Tasks);
    }

    [Fact]
    public async Task Create_rejects_missing_assignee_without_partial_write()
    {
        var repository = new FakeTaskRepository { UserExists = false };
        var service = new TaskService(repository);

        await Assert.ThrowsAsync<TaskResourceNotFoundException>(() =>
            service.CreateAsync("Task", 99, null));
        Assert.Empty(repository.Tasks);
    }

    [Fact]
    public async Task GetById_returns_existing_task()
    {
        var expected = new TaskItem(7, "Task", TaskState.Pending, 1, DateTimeOffset.UtcNow, null);

        var result = await new TaskService(new FakeTaskRepository(expected)).GetByIdAsync(7);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetById_rejects_invalid_id()
    {
        await Assert.ThrowsAsync<TaskValidationException>(() =>
            new TaskService(new FakeTaskRepository()).GetByIdAsync(0));
    }

    [Fact]
    public async Task GetById_rejects_missing_task()
    {
        await Assert.ThrowsAsync<TaskResourceNotFoundException>(() =>
            new TaskService(new FakeTaskRepository()).GetByIdAsync(7));
    }
}

internal sealed class FakeTaskRepository(params TaskItem[] tasks) : ITaskRepository
{
    public bool UserExists { get; set; } = true;
    public List<TaskItem> Tasks { get; } = [.. tasks];
    public TaskFilter? LastFilter { get; private set; }
    public Task<bool> UserExistsAsync(int id, CancellationToken token) => Task.FromResult(UserExists);
    public Task<TaskItem> AddAsync(TaskItem task, CancellationToken token)
    {
        var saved = task with { Id = Tasks.Count + 1 };
        Tasks.Add(saved);
        return Task.FromResult(saved);
    }
    public Task<IReadOnlyList<TaskItem>> ListAsync(TaskFilter filter, CancellationToken token)
    {
        LastFilter = filter;
        return Task.FromResult<IReadOnlyList<TaskItem>>([.. Tasks]);
    }
    public Task<TaskItem?> FindByIdAsync(int id, CancellationToken token) =>
        Task.FromResult(Tasks.SingleOrDefault(task => task.Id == id));
    public Task<TaskItem> UpdateStatusAsync(TaskItem task, CancellationToken token)
    {
        var index = Tasks.FindIndex(existing => existing.Id == task.Id);
        if (index >= 0) Tasks[index] = task;
        return Task.FromResult(task);
    }

    public Task<TaskItem> UpdateAdditionalInfoAsync(TaskItem task, CancellationToken token)
    {
        var index = Tasks.FindIndex(existing => existing.Id == task.Id);
        if (index >= 0) Tasks[index] = task;
        return Task.FromResult(task);
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset time) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => time;
}
