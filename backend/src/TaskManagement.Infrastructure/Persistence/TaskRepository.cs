using Microsoft.EntityFrameworkCore;
using TaskManagement.Core.Tasks;

namespace TaskManagement.Infrastructure.Persistence;

public sealed class TaskRepository(TaskManagementDbContext context) : ITaskRepository
{
    public async Task<bool> UserExistsAsync(int id, CancellationToken token)
    {
        try
        {
            return await context.Users.AnyAsync(user => user.Id == id, token);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { throw new TaskPersistenceException(exception); }
    }

    public async Task<TaskItem> AddAsync(TaskItem task, CancellationToken token)
    {
        var row = new TaskRow
        {
            Title = task.Title, Status = task.Status, UserId = task.UserId,
            CreatedAt = task.CreatedAt, AdditionalInfo = task.AdditionalInfo
        };
        context.Add(row);
        try
        {
            await context.SaveChangesAsync(token);
            return task with { Id = row.Id };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { throw new TaskPersistenceException(exception); }
    }

    public async Task<IReadOnlyList<TaskItem>> ListAsync(TaskFilter filter, CancellationToken token)
    {
        try
        {
            var query = context.Set<TaskRow>().AsNoTracking();
            if (filter.UserId is not null) query = query.Where(row => row.UserId == filter.UserId);
            if (filter.Status is not null) query = query.Where(row => row.Status == filter.Status);
            if (filter.Priority is not null) query = query.Where(row =>
                TaskManagementDbContext.JsonValue(row.AdditionalInfo, "$.priority") == filter.Priority);
            return await query.OrderByDescending(row => row.CreatedAt).ThenByDescending(row => row.Id)
                .Select(row => new TaskItem(row.Id, row.Title, row.Status, row.UserId,
                    row.CreatedAt, row.AdditionalInfo)).ToListAsync(token);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { throw new TaskPersistenceException(exception); }
    }

    public async Task<TaskItem?> FindByIdAsync(int id, CancellationToken token)
    {
        try
        {
            return await context.Set<TaskRow>().AsNoTracking()
                .Where(row => row.Id == id)
                .Select(row => new TaskItem(row.Id, row.Title, row.Status, row.UserId, row.CreatedAt, row.AdditionalInfo))
                .SingleOrDefaultAsync(token);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { throw new TaskPersistenceException(exception); }
    }

    public async Task<TaskItem> UpdateStatusAsync(TaskItem task, CancellationToken token)
    {
        try
        {
            var row = await context.Set<TaskRow>().SingleOrDefaultAsync(existing => existing.Id == task.Id, token)
                ?? throw new TaskResourceNotFoundException("La tarea solicitada no existe.");
            row.Status = task.Status;
            await context.SaveChangesAsync(token);
            return task;
        }
        catch (OperationCanceledException) { throw; }
        catch (TaskResourceNotFoundException) { throw; }
        catch (Exception exception) { throw new TaskPersistenceException(exception); }
    }

    public async Task<TaskItem> UpdateAdditionalInfoAsync(TaskItem task, CancellationToken token)
    {
        try
        {
            var row = await context.Set<TaskRow>().SingleOrDefaultAsync(existing => existing.Id == task.Id, token)
                ?? throw new TaskResourceNotFoundException("La tarea solicitada no existe.");
            row.AdditionalInfo = task.AdditionalInfo;
            await context.SaveChangesAsync(token);
            return task;
        }
        catch (OperationCanceledException) { throw; }
        catch (TaskResourceNotFoundException) { throw; }
        catch (Exception exception) { throw new TaskPersistenceException(exception); }
    }
}
