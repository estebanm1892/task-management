using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskManagement.Tests.Database;

namespace TaskManagement.Tests.E2E;

[CollectionDefinition("E2E")]
public sealed class E2eCollection : ICollectionFixture<E2eApiFixture>;

public sealed class E2eApiFixture : IAsyncLifetime
{
    private readonly SqlServerFixture _database = new();
    private WebApplicationFactory<Program>? _factory;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:TaskManagement", _database.ConnectionString));
        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        _factory?.Dispose();
        await _database.DisposeAsync();
    }

    public async Task<UserDto> CreateUserAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/users", new
        {
            name = "E2E User",
            email = $"e2e-{Guid.NewGuid():N}@example.com"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserDto>())!;
    }

    public async Task<TaskDto> CreateTaskAsync(int userId, string title)
    {
        var response = await Client.PostAsJsonAsync("/api/tasks", new
        {
            title,
            userId,
            additionalInfo = "{\"priority\":\"Medium\"}"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskDto>())!;
    }
}

public sealed record UserDto(int Id, string Name, string Email);
public sealed record TaskDto(
    int Id, string Title, string Status, int UserId, DateTimeOffset CreatedAt, string? AdditionalInfo);
