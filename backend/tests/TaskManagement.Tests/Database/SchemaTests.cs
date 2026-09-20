namespace TaskManagement.Tests.Database;

public sealed class SchemaTests
{
    [Fact]
    public void Script_defines_users_and_tasks_tables()
    {
        var sql = SqlScript.Read();

        Assert.Contains("CREATE TABLE dbo.Users", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE TABLE dbo.Tasks", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Tables_define_primary_keys()
    {
        var sql = SqlScript.Read();

        Assert.Contains("CONSTRAINT PK_Users PRIMARY KEY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CONSTRAINT PK_Tasks PRIMARY KEY", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Tasks_reference_users()
    {
        var sql = SqlScript.Read();

        Assert.Contains("CONSTRAINT FK_Tasks_Users", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("REFERENCES dbo.Users (Id)", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Normalized_email_has_a_unique_index()
    {
        var sql = SqlScript.Read();

        Assert.Contains("CREATE UNIQUE INDEX UX_Users_NormalizedEmail", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Task_status_is_restricted_to_supported_values()
    {
        var sql = SqlScript.Read();

        Assert.Contains("CONSTRAINT CK_Tasks_Status CHECK", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'Pending', 'InProgress', 'Done'", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Additional_information_requires_valid_json_and_priority()
    {
        var sql = SqlScript.Read();

        Assert.Contains("ISJSON(AdditionalInfo) = 1", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'Low', 'Medium', 'High'", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Task_lookup_has_a_deterministic_covering_index()
    {
        var sql = SqlScript.Read();

        Assert.Contains("CREATE INDEX IX_Tasks_UserId_Status_CreatedAt", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("(UserId, Status, CreatedAt DESC, Id DESC)", sql, StringComparison.OrdinalIgnoreCase);
    }

    internal static class SqlScript
    {
        public static string Read()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "specs")))
            {
                directory = directory.Parent;
            }

            var root = directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
            return File.ReadAllText(Path.Combine(root, "database", "create.sql"));
        }
    }
}
