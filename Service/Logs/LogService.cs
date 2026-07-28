using System.Globalization;
using AiGateway.Service.Logs.Dtos;
using AiGateway.Service.Logs.Interfaces;
using Microsoft.Data.Sqlite;

namespace AiGateway.Service.Logs;

// Reads directly from the SQLite table Serilog.Sinks.SQLite writes to (App_dbs/AiGatewayLogs.db,
// table "Logs") — there is no EF Core entity for it, since that table's schema is owned by the sink,
// not by this app. The exact column names (id, Timestamp, Level, Exception, RenderedMessage, Properties)
// come from the sink's own CREATE TABLE statement.
public class LogService(string connectionString) : ILogService
{
    // Matches the format Serilog.Sinks.SQLite stores timestamps in (storeTimestampInUtc: true in Program.cs),
    // so it can be used both to parse rows back out and to build sargable string-range WHERE clauses.
    private const string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fff";

    public async Task<LogSearchResultDto> SearchAsync(LogSearchDto query, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var conditions = new List<string>();
        var parameters = new List<(string Name, object Value)>();

        if (!string.IsNullOrWhiteSpace(query.Level))
        {
            conditions.Add("Level = @level COLLATE NOCASE");
            parameters.Add(("@level", query.Level));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            conditions.Add("(RenderedMessage LIKE @search OR Exception LIKE @search)");
            parameters.Add(("@search", $"%{query.Search}%"));
        }

        if (query.From is { } from)
        {
            conditions.Add("Timestamp >= @from");
            parameters.Add(("@from", from.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture)));
        }

        if (query.To is { } to)
        {
            conditions.Add("Timestamp <= @to");
            parameters.Add(("@to", to.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture)));
        }

        var whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : string.Empty;

        var totalCount = await CountAsync(connection, whereClause, parameters, cancellationToken);
        var entries = await FetchPageAsync(connection, whereClause, parameters, query.Page, query.PageSize, cancellationToken);

        return new LogSearchResultDto
        {
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            Entries = entries
        };
    }

    private static async Task<int> CountAsync(
        SqliteConnection connection,
        string whereClause,
        List<(string Name, object Value)> parameters,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM Logs {whereClause}";
        AddParameters(command, parameters);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private static async Task<List<LogEntryDto>> FetchPageAsync(
        SqliteConnection connection,
        string whereClause,
        List<(string Name, object Value)> parameters,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT id, Timestamp, Level, Exception, RenderedMessage, Properties
            FROM Logs
            {whereClause}
            ORDER BY id DESC
            LIMIT @pageSize OFFSET @offset
            """;
        AddParameters(command, parameters);
        command.Parameters.AddWithValue("@pageSize", pageSize);
        command.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

        var entries = new List<LogEntryDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(new LogEntryDto
            {
                Id = reader.GetInt64(0),
                Timestamp = ParseTimestamp(reader.GetString(1)),
                Level = reader.GetString(2),
                Exception = reader.IsDBNull(3) ? null : reader.GetString(3),
                Message = reader.IsDBNull(4) ? null : reader.GetString(4),
                Properties = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return entries;
    }

    private static void AddParameters(SqliteCommand command, List<(string Name, object Value)> parameters)
    {
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }
    }

    private static DateTimeOffset ParseTimestamp(string raw) =>
        new(DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal), TimeSpan.Zero);
}
