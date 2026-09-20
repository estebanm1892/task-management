using Microsoft.EntityFrameworkCore;
using TaskManagement.Core.Tasks;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Tests.Database;
namespace TaskManagement.Tests.Tasks;
public sealed class TaskRepositoryWriteTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
{
    [Fact]
    public async Task Add_is_durable_and_invalid_json_writes_nothing()
    {
        var baseline = await database.ScalarAsync<int>("SELECT COUNT(*) FROM dbo.Tasks;");
        await using (var context = TaskRepositoryContext.Create(database.ConnectionString))
        {
            var saved = await new TaskRepository(context).AddAsync(
                new TaskItem(0, "Persisted", TaskState.Pending, 1,
                    DateTimeOffset.UtcNow, "{\"priority\":\"High\"}"), default);
            Assert.True(saved.Id > 0);
        }
        Assert.Equal(baseline + 1, await database.ScalarAsync<int>("SELECT COUNT(*) FROM dbo.Tasks;"));
        await using var invalidContext = TaskRepositoryContext.Create(database.ConnectionString);
        await Assert.ThrowsAsync<TaskPersistenceException>(() => new TaskRepository(invalidContext).AddAsync(
            new TaskItem(0, "Invalid", TaskState.Pending, 1, DateTimeOffset.UtcNow, "not-json"), default));
        Assert.Equal(baseline + 1, await database.ScalarAsync<int>("SELECT COUNT(*) FROM dbo.Tasks;"));
    }
}
internal static class TaskRepositoryContext
{
    public static TaskManagementDbContext Create(string connection) => new(
        new DbContextOptionsBuilder<TaskManagementDbContext>().UseSqlServer(connection).Options);
}
