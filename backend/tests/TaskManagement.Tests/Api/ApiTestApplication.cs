using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskManagement.Core.Tasks;
using TaskManagement.Core.Users;

namespace TaskManagement.Tests.Api;

internal sealed class ApiTestApplication(Action<IServiceCollection>? configure = null)
    : WebApplicationFactory<Program>
{
    public InMemoryUserRepository Users { get; } = new();
    public InMemoryTaskRepository Tasks { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureServices(services =>
    {
        services.RemoveAll<IUserRepository>();
        services.RemoveAll<ITaskRepository>();
        services.AddSingleton<IUserRepository>(Users);
        services.AddSingleton<ITaskRepository>(Tasks);
        configure?.Invoke(services);
    });
}

internal static class HttpResponseAssertions
{
    public static async Task<string?> ProblemDetailAsync(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<Dictionary<string, object?>>())?["detail"]?.ToString();
}

internal sealed class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = [];
    private int _nextId = 1;
    public bool FailReads { get; set; }
    public bool FailWrites { get; set; }
    public IReadOnlyList<User> Snapshot => [.. _users];

    public Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        if (FailReads) throw new UserPersistenceException(new InvalidOperationException("database down"));
        return Task.FromResult(_users.Any(user => user.NormalizedEmail == normalizedEmail));
    }

    public Task<User> AddAsync(User user, CancellationToken cancellationToken)
    {
        if (FailWrites) throw new UserPersistenceException(new InvalidOperationException("database down"));
        if (_users.Any(existing => existing.NormalizedEmail == user.NormalizedEmail))
            throw new DuplicateUserEmailException();
        var saved = user with { Id = _nextId++ };
        _users.Add(saved);
        return Task.FromResult(saved);
    }

    public Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken)
    {
        if (FailReads) throw new UserPersistenceException(new InvalidOperationException("database down"));
        return Task.FromResult<IReadOnlyList<User>>([.. _users]);
    }
}

internal sealed class InMemoryTaskRepository : ITaskRepository
{
    private readonly List<TaskItem> _tasks = [];
    private int _nextId = 1;
    public HashSet<int> ExistingUsers { get; } = [1];
    public bool FailReads { get; set; }
    public bool FailWrites { get; set; }
    public IReadOnlyList<TaskItem> Snapshot => [.. _tasks];

    public Task<bool> UserExistsAsync(int id, CancellationToken token)
    {
        if (FailReads) throw new TaskPersistenceException(new InvalidOperationException("database down"));
        return Task.FromResult(ExistingUsers.Contains(id));
    }

    public Task<TaskItem> AddAsync(TaskItem task, CancellationToken token)
    {
        if (FailWrites) throw new TaskPersistenceException(new InvalidOperationException("database down"));
        var saved = task with { Id = _nextId++ };
        _tasks.Add(saved);
        return Task.FromResult(saved);
    }

    public Task<IReadOnlyList<TaskItem>> ListAsync(TaskFilter filter, CancellationToken token)
    {
        if (FailReads) throw new TaskPersistenceException(new InvalidOperationException("database down"));
        var query = _tasks.AsEnumerable();
        if (filter.UserId is not null) query = query.Where(task => task.UserId == filter.UserId);
        if (filter.Status is not null) query = query.Where(task => task.Status == filter.Status);
        if (filter.Priority is not null) query = query.Where(task => task.AdditionalInfo?.Contains($"\"priority\":\"{filter.Priority}\"") == true);
        return Task.FromResult<IReadOnlyList<TaskItem>>(query.OrderByDescending(task => task.CreatedAt).ThenByDescending(task => task.Id).ToList());
    }

    public Task<TaskItem?> FindByIdAsync(int id, CancellationToken token)
    {
        if (FailReads) throw new TaskPersistenceException(new InvalidOperationException("database down"));
        return Task.FromResult(_tasks.SingleOrDefault(task => task.Id == id));
    }

    public Task<TaskItem> UpdateStatusAsync(TaskItem task, CancellationToken token)
    {
        if (FailWrites) throw new TaskPersistenceException(new InvalidOperationException("database down"));
        var index = _tasks.FindIndex(existing => existing.Id == task.Id);
        if (index < 0) throw new TaskResourceNotFoundException("La tarea solicitada no existe.");
        _tasks[index] = task;
        return Task.FromResult(task);
    }

    public Task<TaskItem> UpdateAdditionalInfoAsync(TaskItem task, CancellationToken token)
    {
        if (FailWrites) throw new TaskPersistenceException(new InvalidOperationException("database down"));
        var index = _tasks.FindIndex(existing => existing.Id == task.Id);
        if (index < 0) throw new TaskResourceNotFoundException("La tarea solicitada no existe.");
        _tasks[index] = task;
        return Task.FromResult(task);
    }
}
