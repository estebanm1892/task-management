namespace TaskManagement.Core.Tasks;
public static class TaskTransitions
{
    public static TaskItem Apply(TaskItem task, string requestedStatus)
    {
        if (!Enum.TryParse<TaskState>(requestedStatus, false, out var requested))
            throw new TaskTransitionException("El estado solicitado no es válido.");

        var allowed = task.Status switch
        {
            TaskState.Pending => requested == TaskState.InProgress,
            TaskState.InProgress => requested == TaskState.Done,
            _ => false
        };

        return allowed
            ? task with { Status = requested }
            : throw new TaskTransitionException("La transición de estado solicitada no está permitida.");
    }
}
