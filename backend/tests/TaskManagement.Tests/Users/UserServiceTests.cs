using TaskManagement.Core.Users;

namespace TaskManagement.Tests.Users;

public sealed class UserServiceTests
{
    [Fact]
    public async Task Create_trims_values_normalizes_email_and_persists_user()
    {
        var repository = new FakeUserRepository();
        var service = new UserService(repository);

        var user = await service.CreateAsync("  Ada Lovelace  ", "  Ada@Example.COM  ");

        Assert.Equal("Ada Lovelace", user.Name);
        Assert.Equal("Ada@Example.COM", user.Email);
        Assert.Equal("ada@example.com", user.NormalizedEmail);
        Assert.Equal([user], await service.ListAsync());
    }

    [Theory]
    [InlineData("", "user@example.com")]
    [InlineData("   ", "user@example.com")]
    [InlineData("User", "")]
    [InlineData("User", "not-an-email")]
    public async Task Create_rejects_missing_or_invalid_values(string name, string email)
    {
        var repository = new FakeUserRepository();
        var service = new UserService(repository);

        await Assert.ThrowsAsync<UserValidationException>(() => service.CreateAsync(name, email));
        Assert.Empty(repository.Users);
    }

    [Fact]
    public async Task Create_rejects_values_that_exceed_contract_lengths()
    {
        var service = new UserService(new FakeUserRepository());

        await Assert.ThrowsAsync<UserValidationException>(() =>
            service.CreateAsync(new string('N', 121), "user@example.com"));
        await Assert.ThrowsAsync<UserValidationException>(() =>
            service.CreateAsync("User", $"{new string('e', 243)}@example.com"));
    }

    [Fact]
    public async Task Create_rejects_equivalent_email_without_persisting()
    {
        var existing = new User(1, "Existing", "user@example.com", "user@example.com");
        var repository = new FakeUserRepository(existing);
        var service = new UserService(repository);

        await Assert.ThrowsAsync<DuplicateUserEmailException>(() =>
            service.CreateAsync("Duplicate", "  USER@EXAMPLE.COM "));

        Assert.Equal([existing], repository.Users);
    }

    [Fact]
    public async Task List_returns_an_empty_collection_or_all_persisted_users()
    {
        var emptyService = new UserService(new FakeUserRepository());
        var users = new[]
        {
            new User(1, "Ada", "ada@example.com", "ada@example.com"),
            new User(2, "Grace", "grace@example.com", "grace@example.com")
        };
        var populatedService = new UserService(new FakeUserRepository(users));

        Assert.Empty(await emptyService.ListAsync());
        Assert.Equal(users, await populatedService.ListAsync());
    }

    [Fact]
    public async Task GetById_returns_existing_user()
    {
        var expected = new User(7, "Ada", "ada@example.com", "ada@example.com");

        var result = await new UserService(new FakeUserRepository(expected)).GetByIdAsync(7);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetById_rejects_invalid_id()
    {
        await Assert.ThrowsAsync<UserValidationException>(() =>
            new UserService(new FakeUserRepository()).GetByIdAsync(0));
    }

    [Fact]
    public async Task GetById_rejects_missing_user()
    {
        await Assert.ThrowsAsync<UserResourceNotFoundException>(() =>
            new UserService(new FakeUserRepository()).GetByIdAsync(7));
    }

    private sealed class FakeUserRepository(params User[] users) : IUserRepository
    {
        public List<User> Users { get; } = [.. users];

        public Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
            Task.FromResult(Users.Any(user => user.NormalizedEmail == normalizedEmail));

        public Task<User?> FindByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(Users.SingleOrDefault(user => user.Id == id));

        public Task<User> AddAsync(User user, CancellationToken cancellationToken)
        {
            var persisted = user with { Id = Users.Count + 1 };
            Users.Add(persisted);
            return Task.FromResult(persisted);
        }

        public Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<User>>([.. Users]);
    }
}
