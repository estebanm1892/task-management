using System.Text.Json;

namespace TaskManagement.Core.Tasks;

public enum TaskState { Pending, InProgress, Done }

public sealed record TaskItem(
    int Id, string Title, TaskState Status, int UserId,
    DateTimeOffset CreatedAt, string? AdditionalInfo);

public sealed record TaskFilter(int? UserId, TaskState? Status, string? Priority);

public interface ITaskRepository
{
    Task<bool> UserExistsAsync(int id, CancellationToken token);
    Task<TaskItem> AddAsync(TaskItem task, CancellationToken token);
    Task<IReadOnlyList<TaskItem>> ListAsync(TaskFilter filter, CancellationToken token);
    Task<TaskItem?> FindByIdAsync(int id, CancellationToken token);
    Task<TaskItem> UpdateStatusAsync(TaskItem task, CancellationToken token);
    Task<TaskItem> UpdateAdditionalInfoAsync(TaskItem task, CancellationToken token);
}

public sealed class TaskService(ITaskRepository repository, TimeProvider? timeProvider = null)
{
    private static readonly HashSet<string> Priorities = ["Low", "Medium", "High"];
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<TaskItem> CreateAsync(
        string? title, int userId, string? additionalInfo, CancellationToken token = default)
    {
        var trimmedTitle = title?.Trim() ?? string.Empty;
        if (trimmedTitle.Length is 0 or > 200 || userId <= 0)
        {
            throw new TaskValidationException("El título y el colaborador asignado son obligatorios.");
        }

        ValidateAdditionalInfo(additionalInfo);
        if (!await repository.UserExistsAsync(userId, token))
        {
            throw new TaskResourceNotFoundException("El colaborador asignado no existe.");
        }

        return await repository.AddAsync(
            new TaskItem(0, trimmedTitle, TaskState.Pending, userId,
                _timeProvider.GetUtcNow(), additionalInfo), token);
    }

    public async Task<IReadOnlyList<TaskItem>> ListAsync(
        int? userId, string? status, string? priority, CancellationToken token = default)
    {
        TaskState? parsedStatus = null;
        if (status is not null)
        {
            if (!Enum.TryParse<TaskState>(status, false, out var state))
                throw new TaskValidationException("El estado solicitado no es válido.");
            parsedStatus = state;
        }

        if (priority is not null && !Priorities.Contains(priority))
            throw new TaskValidationException("La prioridad solicitada no es válida.");
        if (userId <= 0)
            throw new TaskValidationException("El colaborador solicitado no es válido.");
        if (userId is not null && !await repository.UserExistsAsync(userId.Value, token))
            throw new TaskResourceNotFoundException("El colaborador solicitado no existe.");

        return await repository.ListAsync(new TaskFilter(userId, parsedStatus, priority), token);
    }

    public async Task<TaskItem> ChangeStatusAsync(int id, string? status, CancellationToken token = default)
    {
        if (id <= 0) throw new TaskValidationException("La tarea solicitada no es válida.");
        if (string.IsNullOrWhiteSpace(status)) throw new TaskValidationException("El estado solicitado no es válido.");

        var task = await repository.FindByIdAsync(id, token)
            ?? throw new TaskResourceNotFoundException("La tarea solicitada no existe.");
        var updated = TaskTransitions.Apply(task, status.Trim());
        return await repository.UpdateStatusAsync(updated, token);
    }

    public async Task<TaskItem> UpdateAdditionalInfoAsync(
        int id, string? property, JsonElement value, CancellationToken token = default)
    {
        if (id <= 0) throw new TaskValidationException("La tarea solicitada no es válida.");

        var propertyName = property?.Trim() ?? string.Empty;
        if (propertyName.Length is 0 or > 120)
            throw new TaskValidationException("La propiedad JSON solicitada no es válida.");
        if (value.ValueKind == JsonValueKind.Undefined)
            throw new TaskValidationException("El valor JSON solicitado es obligatorio.");
        if (propertyName.Equals("Title", StringComparison.OrdinalIgnoreCase) ||
            propertyName.Equals("Status", StringComparison.OrdinalIgnoreCase) ||
            propertyName.Equals("UserId", StringComparison.OrdinalIgnoreCase) ||
            propertyName.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase))
            throw new TaskValidationException("No se pueden actualizar propiedades esenciales de la tarea.");
        if (propertyName.Equals("priority", StringComparison.OrdinalIgnoreCase) &&
            (value.ValueKind != JsonValueKind.String || !Priorities.Contains(value.GetString() ?? string.Empty)))
            throw new TaskValidationException("La prioridad debe ser Low, Medium o High.");
        if (propertyName.Equals("priority", StringComparison.OrdinalIgnoreCase))
            propertyName = "priority";

        var task = await repository.FindByIdAsync(id, token)
            ?? throw new TaskResourceNotFoundException("La tarea solicitada no existe.");
        var metadata = ParseAdditionalInfo(task.AdditionalInfo);
        metadata[propertyName] = value.Clone();
        var updated = task with { AdditionalInfo = JsonSerializer.Serialize(metadata) };
        return await repository.UpdateAdditionalInfoAsync(updated, token);
    }

    private static void ValidateAdditionalInfo(string? value)
    {
        if (value is null) return;
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new TaskValidationException("La información adicional debe ser un objeto JSON válido.");
            if (document.RootElement.TryGetProperty("priority", out var priority) &&
                (priority.ValueKind != JsonValueKind.String || !Priorities.Contains(priority.GetString()!)))
                throw new TaskValidationException("La prioridad debe ser Low, Medium o High.");
        }
        catch (JsonException)
        {
            throw new TaskValidationException("La información adicional debe contener JSON válido.");
        }
    }

    private static Dictionary<string, JsonElement> ParseAdditionalInfo(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new TaskValidationException("La información adicional debe ser un objeto JSON válido.");
            return document.RootElement.EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone());
        }
        catch (JsonException)
        {
            throw new TaskValidationException("La información adicional debe contener JSON válido.");
        }
    }
}

public sealed class TaskValidationException(string message) : Exception(message);
public sealed class TaskResourceNotFoundException(string message) : Exception(message);
public sealed class TaskTransitionException(string message) : Exception(message);
public sealed class TaskPersistenceException(Exception inner)
    : Exception("No fue posible persistir la información de la tarea.", inner);
