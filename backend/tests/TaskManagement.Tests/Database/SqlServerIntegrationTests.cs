using Microsoft.Data.SqlClient;

namespace TaskManagement.Tests.Database;

public sealed class SqlServerIntegrationTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
{
    [Fact]
    public async Task Script_creates_relational_schema_and_is_idempotent()
    {
        await database.ExecuteScriptAsync();

        Assert.Equal(2, await database.ScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.key_constraints WHERE [type] = 'PK' AND parent_object_id IN (OBJECT_ID('dbo.Users'), OBJECT_ID('dbo.Tasks'));"));
        Assert.Equal(1, await database.ScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.foreign_keys WHERE name = 'FK_Tasks_Users';"));
    }

    [Fact]
    public async Task Schema_enforces_unique_email_foreign_key_status_and_json_constraints()
    {
        Assert.Equal(1, await database.ScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.indexes WHERE name = 'UX_Users_NormalizedEmail' AND is_unique = 1;"));
        Assert.Equal(1, await database.ScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.indexes WHERE name = 'IX_Tasks_UserId_Status_CreatedAt';"));
        Assert.Equal("UserId ASC,Status ASC,CreatedAt DESC,Id DESC", await database.ScalarAsync<string>("""
            SELECT STRING_AGG(CONCAT(c.name, CASE WHEN ic.is_descending_key = 1 THEN ' DESC' ELSE ' ASC' END), ',')
                WITHIN GROUP (ORDER BY ic.key_ordinal)
            FROM sys.indexes AS i
            JOIN sys.index_columns AS ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.object_id = OBJECT_ID('dbo.Tasks') AND i.name = 'IX_Tasks_UserId_Status_CreatedAt';
            """));

        await Assert.ThrowsAsync<SqlException>(() => database.ExecuteAsync(
            "INSERT dbo.Users (Name, Email, NormalizedEmail) VALUES (N'Duplicate', N'USER@example.com', N'user@example.com');"));
        await Assert.ThrowsAsync<SqlException>(() => database.ExecuteAsync(TaskInsert(999, "'Pending'", "N'{}'")));
        await Assert.ThrowsAsync<SqlException>(() => database.ExecuteAsync(TaskInsert(1, "'Unknown'", "N'{}'")));
        await Assert.ThrowsAsync<SqlException>(() => database.ExecuteAsync(TaskInsert(1, "'Pending'", "N'not-json'")));
        await Assert.ThrowsAsync<SqlException>(() => database.ExecuteAsync(TaskInsert(1, "'Pending'", "N'{\"priority\":\"Urgent\"}'")));
    }

    [Fact]
    public async Task Required_query_filters_by_user_and_status_newest_first()
    {
        const string sql = """
            SELECT Title
            FROM dbo.Tasks
            WHERE UserId = @UserId AND (@Status IS NULL OR Status = @Status)
            ORDER BY CreatedAt DESC, Id DESC;
            """;

        var titles = await database.StringListAsync(sql, ("@UserId", 1), ("@Status", "Pending"));

        Assert.Equal(["Newest pending", "Oldest pending"], titles);
    }

    [Fact]
    public async Task Script_demonstrates_status_filtering_newest_first()
    {
        var titles = await database.ScriptTaskTitlesAsync();

        Assert.Equal(["Newest pending", "Oldest pending"], titles);
    }

    [Fact]
    public async Task Native_json_functions_read_filter_and_expand_task_metadata()
    {
        const string jsonSql = """
            SELECT JSON_VALUE(AdditionalInfo, '$.priority'), JSON_QUERY(AdditionalInfo, '$.tags')
            FROM dbo.Tasks
            WHERE JSON_VALUE(AdditionalInfo, '$.priority') = N'High';
            """;

        var json = await database.RowAsync(jsonSql);
        Assert.Equal("High", json[0]);
        Assert.Equal("[\"backend\",\"urgent\"]", json[1]);

        var tags = await database.StringListAsync(
            "SELECT j.[value] FROM dbo.Tasks AS t CROSS APPLY OPENJSON(t.AdditionalInfo, '$.tags') AS j WHERE t.Title = N'Oldest pending' ORDER BY j.[key];");
        Assert.Equal(["backend", "urgent"], tags);
    }

    [Fact]
    public async Task Native_json_modify_updates_only_the_requested_property()
    {
        const string sql = """
            DECLARE @Metadata NVARCHAR(MAX) = N'{"priority":"Medium","tags":["backend"]}';
            SELECT JSON_MODIFY(@Metadata, '$.priority', N'High');
            """;

        var updated = await database.ScalarAsync<string>(sql);

        Assert.Equal("{\"priority\":\"High\",\"tags\":[\"backend\"]}", updated);
    }

    private static string TaskInsert(int userId, string status, string additionalInfo) =>
        $"INSERT dbo.Tasks (Title, Status, UserId, CreatedAt, AdditionalInfo) VALUES (N'Invalid', {status}, {userId}, SYSUTCDATETIME(), {additionalInfo});";
}

public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string MasterConnection = @"Server=.\SQLEXPRESS;Integrated Security=True;TrustServerCertificate=True;Initial Catalog=master;Connection Timeout=15";
    private readonly string _databaseName = $"TaskManagementTests_{Guid.NewGuid():N}";

    public string ConnectionString => new SqlConnectionStringBuilder(MasterConnection)
    {
        InitialCatalog = _databaseName
    }.ConnectionString;

    public async Task InitializeAsync()
    {
        await RecreateDatabaseAsync();
        await ExecuteScriptAsync();
        await ExecuteAsync("INSERT dbo.Users (Name, Email, NormalizedEmail) VALUES (N'Test User', N'user@example.com', N'user@example.com');");
        await ExecuteAsync("INSERT dbo.Users (Name, Email, NormalizedEmail) VALUES (N'Other User', N'other@example.com', N'other@example.com');");
        await ExecuteAsync("""
            INSERT dbo.Tasks (Title, Status, UserId, CreatedAt, AdditionalInfo) VALUES
                (N'Oldest pending', N'Pending', 1, '2026-01-01T00:00:00Z', N'{"priority":"High","tags":["backend","urgent"]}'),
                (N'Newest pending', N'Pending', 1, '2026-02-01T00:00:00Z', N'{"priority":"Low","tags":["frontend"]}'),
                (N'Completed', N'Done', 1, '2026-03-01T00:00:00Z', NULL),
                (N'Other user task', N'Pending', 2, '2026-04-01T00:00:00Z', N'{"priority":"Medium"}');
            """);
    }

    public async Task DisposeAsync() => await DropDatabaseAsync();

    public async Task ExecuteScriptAsync() => await ExecuteAsync(SchemaTests.SqlScript.Read());

    public async Task ExecuteAsync(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
        await command.ExecuteNonQueryAsync();
    }

    public async Task<T> ScalarAsync<T>(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        return Assert.IsType<T>(await command.ExecuteScalarAsync());
    }

    public async Task<string[]> StringListAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }

        var values = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }

        return [.. values];
    }

    public async Task<string[]> ScriptTaskTitlesAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(SchemaTests.SqlScript.Read(), connection) { CommandTimeout = 30 };
        await using var reader = await command.ExecuteReaderAsync();
        var titles = new List<string>();
        while (await reader.ReadAsync())
        {
            titles.Add(reader.GetString(1));
        }

        return [.. titles];
    }

    public async Task<object[]> RowAsync(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return [reader.GetValue(0), reader.GetValue(1)];
    }

    private async Task RecreateDatabaseAsync()
    {
        await DropDatabaseAsync();
        await using var connection = new SqlConnection(MasterConnection);
        await connection.OpenAsync();
        await using var command = new SqlCommand($"CREATE DATABASE [{_databaseName}];", connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task DropDatabaseAsync()
    {
        await using var connection = new SqlConnection(MasterConnection);
        await connection.OpenAsync();
        var sql = $"IF DB_ID(N'{_databaseName}') IS NOT NULL BEGIN ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]; END;";
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
