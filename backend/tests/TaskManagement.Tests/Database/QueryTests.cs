namespace TaskManagement.Tests.Database;

public sealed class QueryTests
{
    [Fact]
    public void Script_queries_tasks_by_user_and_optional_status_in_deterministic_order()
    {
        var sql = SchemaTests.SqlScript.Read();

        Assert.Contains("WHERE t.UserId = @UserId", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("(@Status IS NULL OR t.Status = @Status)", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY t.CreatedAt DESC, t.Id DESC", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Script_reads_and_filters_priority_with_json_value()
    {
        var sql = SchemaTests.SqlScript.Read();

        Assert.Contains("JSON_VALUE(t.AdditionalInfo, '$.priority') AS Priority", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JSON_VALUE(t.AdditionalInfo, '$.priority') = @Priority", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Script_reads_tags_as_a_json_array()
    {
        var sql = SchemaTests.SqlScript.Read();

        Assert.Contains("JSON_QUERY(t.AdditionalInfo, '$.tags') AS Tags", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Script_expands_tags_with_open_json()
    {
        var sql = SchemaTests.SqlScript.Read();

        Assert.Contains("CROSS APPLY OPENJSON(t.AdditionalInfo, '$.tags')", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Script_demonstrates_updating_a_json_property_with_json_modify()
    {
        var sql = SchemaTests.SqlScript.Read();

        Assert.Contains("JSON_MODIFY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'$.priority'", sql, StringComparison.OrdinalIgnoreCase);
    }
}
