using TaskManagement.Core.Tasks;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Tests.Database;
namespace TaskManagement.Tests.Tasks;
public sealed class TaskRepositoryQueryTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
{
    [Fact]
    public async Task List_combines_filters_and_orders_by_date_then_id_descending()
    {
        var time = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await using (var write = TaskRepositoryContext.Create(database.ConnectionString))
        {
            var repository = new TaskRepository(write);
            await repository.AddAsync(new(0, "Tie first", TaskState.Pending, 1, time, "{\"priority\":\"High\"}"), default);
            await repository.AddAsync(new(0, "Tie second", TaskState.Pending, 1, time, "{\"priority\":\"High\"}"), default);
        }
        await using var read = TaskRepositoryContext.Create(database.ConnectionString);
        var tasks = await new TaskRepository(read).ListAsync(
            new(1, TaskState.Pending, "High"), default);
        Assert.Equal(["Tie second", "Tie first", "Oldest pending"], tasks.Select(task => task.Title));
    }
}
