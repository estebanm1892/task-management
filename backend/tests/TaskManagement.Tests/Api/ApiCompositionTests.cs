using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Core.Tasks;
using TaskManagement.Core.Users;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Tests.Api;

public sealed class ApiCompositionTests
{
    [Fact]
    public async Task Program_registers_controllers_problem_details_services_repositories_and_ef_context()
    {
        await using var application = new ApiTestApplication();
        using var scope = application.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<UserService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<TaskService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUserRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ITaskRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>());

        var response = await application.CreateClient().GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(await response.Content.ReadFromJsonAsync<object[]>() ?? []);
    }
}
