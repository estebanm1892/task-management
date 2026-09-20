using TaskManagement.Core.Tasks;

namespace TaskManagement.Api.Tasks;

public sealed record CreateTaskRequest(string? Title, int UserId, string? AdditionalInfo)
{
    public (string? Title, int UserId, string? AdditionalInfo) ToServiceInput() =>
        (Title, UserId, AdditionalInfo);
}

public sealed record UpdateTaskStatusRequest(string? Status);

public sealed record UpdateTaskAdditionalInfoRequest(string? Property, System.Text.Json.JsonElement Value);

public sealed record TaskResponse(
    int Id,
    string Title,
    string Status,
    int UserId,
    DateTimeOffset CreatedAt,
    string? AdditionalInfo);

public static class TaskMappings
{
    public static TaskResponse ToResponse(this TaskItem task) => new(
        task.Id, task.Title, task.Status.ToString(), task.UserId, task.CreatedAt, task.AdditionalInfo);
}
