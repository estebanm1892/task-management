using Microsoft.EntityFrameworkCore;

namespace TaskManagement.Infrastructure.Persistence;

public sealed class TaskManagementDbContext(DbContextOptions<TaskManagementDbContext> options)
    : DbContext(options)
{
    internal DbSet<UserRow> Users => Set<UserRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<UserRow>();
        user.ToTable("Users");
        user.HasKey(row => row.Id).HasName("PK_Users");
        user.Property(row => row.Id).ValueGeneratedOnAdd();
        user.Property(row => row.Name).HasMaxLength(120).IsRequired();
        user.Property(row => row.Email).HasMaxLength(254).IsRequired();
        user.Property(row => row.NormalizedEmail).HasMaxLength(254).IsRequired();
        user.HasIndex(row => row.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("UX_Users_NormalizedEmail");

        var task = modelBuilder.Entity<TaskRow>();
        task.ToTable("Tasks");
        task.HasKey(row => row.Id).HasName("PK_Tasks");
        task.Property(row => row.Status).HasConversion<string>().HasMaxLength(20);
        task.Property(row => row.Title).HasMaxLength(200).IsRequired();
        task.Property(row => row.CreatedAt).HasConversion(
            value => value.UtcDateTime,
            value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));
        task.HasOne<UserRow>().WithMany().HasForeignKey(row => row.UserId);
        modelBuilder.HasDbFunction(typeof(TaskManagementDbContext).GetMethod(nameof(JsonValue))!)
            .HasName("JSON_VALUE").IsBuiltIn();
    }

    public static string? JsonValue(string? json, string path) => throw new NotSupportedException();
}

internal sealed class TaskRow
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public TaskManagement.Core.Tasks.TaskState Status { get; set; }
    public int UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? AdditionalInfo { get; set; }
}

internal sealed class UserRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
}
