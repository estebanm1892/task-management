using Microsoft.AspNetCore.Mvc;
using TaskManagement.Core.Tasks;

namespace TaskManagement.Api.Tasks;

[ApiController]
[Route("api/tasks")]
public sealed class TasksController(TaskService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TaskResponse>> CreateAsync(
        CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var (title, userId, additionalInfo) = request.ToServiceInput();
        var task = await service.CreateAsync(title, userId, additionalInfo, cancellationToken);
        return Created($"/api/tasks/{task.Id}", task.ToResponse());
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> ListAsync(
        [FromQuery] int? userId,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        CancellationToken cancellationToken)
    {
        var tasks = await service.ListAsync(userId, status, priority, cancellationToken);
        return Ok(tasks.Select(task => task.ToResponse()).ToArray());
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<TaskResponse>> ChangeStatusAsync(
        int id,
        UpdateTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var task = await service.ChangeStatusAsync(id, request.Status, cancellationToken);
        return Ok(task.ToResponse());
    }

    [HttpPatch("{id:int}/additional-info")]
    public async Task<ActionResult<TaskResponse>> UpdateAdditionalInfoAsync(
        int id,
        UpdateTaskAdditionalInfoRequest request,
        CancellationToken cancellationToken)
    {
        var task = await service.UpdateAdditionalInfoAsync(id, request.Property, request.Value, cancellationToken);
        return Ok(task.ToResponse());
    }
}
