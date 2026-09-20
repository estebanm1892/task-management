using TaskManagement.Core.Tasks;

namespace TaskManagement.Tests.Tasks;

public sealed class TaskTransitionTests
{
    [Theory]
    [InlineData(TaskState.Pending, "InProgress", TaskState.InProgress)]
    [InlineData(TaskState.InProgress, "Done", TaskState.Done)]
    public void Apply_allows_only_forward_transitions(
        TaskState current, string requested, TaskState expected)
    {
        var task = Item(current);

        var changed = TaskTransitions.Apply(task, requested);

        Assert.Equal(expected, changed.Status);
        Assert.Equal(current, task.Status);
    }

    [Theory]
    [InlineData(TaskState.Pending, "Pending")]
    [InlineData(TaskState.Pending, "Done")]
    [InlineData(TaskState.InProgress, "Pending")]
    [InlineData(TaskState.InProgress, "InProgress")]
    [InlineData(TaskState.Done, "Pending")]
    [InlineData(TaskState.Done, "InProgress")]
    [InlineData(TaskState.Done, "Done")]
    [InlineData(TaskState.Pending, "Unknown")]
    public void Apply_rejects_repeat_regress_skip_terminal_and_unknown_without_mutation(
        TaskState current, string requested)
    {
        var task = Item(current);

        Assert.Throws<TaskTransitionException>(() => TaskTransitions.Apply(task, requested));
        Assert.Equal(current, task.Status);
    }

    private static TaskItem Item(TaskState status) =>
        new(1, "Task", status, 1, DateTimeOffset.UnixEpoch, null);
}
