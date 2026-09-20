using TaskManagement.Core.Users;

namespace TaskManagement.Api.Users;

public sealed record CreateUserRequest(string? Name, string? Email)
{
    public (string? Name, string? Email) ToServiceInput() => (Name, Email);
}

public sealed record UserResponse(int Id, string Name, string Email);

public static class UserMappings
{
    public static UserResponse ToResponse(this User user) => new(user.Id, user.Name, user.Email);
}
