using Microsoft.EntityFrameworkCore;
using TaskManagement.Core.Users;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Tests.Database;

namespace TaskManagement.Tests.Users;

public sealed class UserRepositoryTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
{
    [Fact]
    public async Task Add_and_list_persist_explicit_user_mapping()
    {
        await using var context = CreateContext(database.ConnectionString);
        var repository = new UserRepository(context);

        var created = await repository.AddAsync(
            new User(0, "Katherine Johnson", "Katherine@Example.com", "katherine@example.com"), default);
        var users = await repository.ListAsync(default);

        Assert.True(created.Id > 0);
        Assert.Contains(users, user => user == created);
    }

    [Fact]
    public async Task FindById_returns_an_existing_user()
    {
        await using var context = CreateContext(database.ConnectionString);
        var repository = new UserRepository(context);
        var created = await repository.AddAsync(
            new User(0, "Katherine Johnson", "find-by-id@example.com", "find-by-id@example.com"), default);

        var result = await repository.FindByIdAsync(created.Id, default);

        Assert.Equal(created, result);
    }

    [Fact]
    public async Task Concurrent_equivalent_emails_allow_only_one_persisted_user()
    {
        var normalizedEmail = $"concurrent-{Guid.NewGuid():N}@example.com";
        await using var firstContext = CreateContext(database.ConnectionString);
        await using var secondContext = CreateContext(database.ConnectionString);
        var attempts = new[]
        {
            new UserRepository(firstContext).AddAsync(new User(0, "First", normalizedEmail, normalizedEmail), default),
            new UserRepository(secondContext).AddAsync(new User(0, "Second", normalizedEmail.ToUpperInvariant(), normalizedEmail), default)
        };

        try
        {
            await Task.WhenAll(attempts);
        }
        catch (DuplicateUserEmailException)
        {
        }

        _ = Assert.Single(attempts, task => task.IsCompletedSuccessfully);
        _ = Assert.Single(attempts, task => task.Exception?.InnerException is DuplicateUserEmailException);
        await using var verificationContext = CreateContext(database.ConnectionString);
        var users = await new UserRepository(verificationContext).ListAsync(default);
        Assert.Single(users, user => user.NormalizedEmail == normalizedEmail);
    }

    [Fact]
    public async Task Persistence_failure_is_translated_without_exposing_provider_details()
    {
        var invalidConnection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(database.ConnectionString)
        {
            InitialCatalog = $"Missing_{Guid.NewGuid():N}",
            ConnectTimeout = 1
        }.ConnectionString;
        await using var context = CreateContext(invalidConnection);
        var repository = new UserRepository(context);

        var exception = await Assert.ThrowsAsync<UserPersistenceException>(() => repository.ListAsync(default));

        Assert.Equal("No fue posible persistir la información del colaborador.", exception.Message);

        await using var writeContext = CreateContext(invalidConnection);
        var writeRepository = new UserRepository(writeContext);
        var writeException = await Assert.ThrowsAsync<UserPersistenceException>(() =>
            writeRepository.AddAsync(new User(0, "User", "user@example.com", "user@example.com"), default));
        Assert.Equal(exception.Message, writeException.Message);
    }

    private static TaskManagementDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new TaskManagementDbContext(options);
    }
}
