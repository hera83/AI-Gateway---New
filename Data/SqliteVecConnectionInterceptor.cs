using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AiGateway.Data;

/// <summary>
/// Loads the sqlite-vec (vec0) native extension on every physical SQLite connection EF Core opens,
/// and attaches the separate vector database file (see <paramref name="vectorDbPath"/>) under the
/// schema name "vectors" — this is what keeps the vec0 virtual table (vec_chunks) out of the main
/// AiGateway.db file, so heavy vector read/write traffic can't slow down API-key/knowledge-metadata
/// queries. Both steps are per-connection SQLite state, not persisted in the database file itself, so
/// they must be redone every time EF Core opens a new physical connection. Microsoft.Data.Sqlite pools
/// physical connections though, so a "new" logical open can hand back a pooled connection that already
/// has "vectors" attached from a previous open — attaching again would throw "database vectors is
/// already in use", hence the pragma_database_list check before attaching.
/// </summary>
public class SqliteVecConnectionInterceptor(string vectorDbPath) : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        var sqliteConnection = (SqliteConnection)connection;
        sqliteConnection.LoadVector();
        AttachVectorDatabase(sqliteConnection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        var sqliteConnection = (SqliteConnection)connection;
        sqliteConnection.LoadVector();
        await AttachVectorDatabaseAsync(sqliteConnection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private void AttachVectorDatabase(SqliteConnection connection)
    {
        if (IsVectorDatabaseAttached(connection))
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = "ATTACH DATABASE $path AS vectors;";
        command.Parameters.AddWithValue("$path", vectorDbPath);
        command.ExecuteNonQuery();
    }

    private async Task AttachVectorDatabaseAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        if (await IsVectorDatabaseAttachedAsync(connection, cancellationToken))
        {
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "ATTACH DATABASE $path AS vectors;";
        command.Parameters.AddWithValue("$path", vectorDbPath);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static bool IsVectorDatabaseAttached(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pragma_database_list WHERE name = 'vectors';";
        return (long)command.ExecuteScalar()! > 0;
    }

    private static async Task<bool> IsVectorDatabaseAttachedAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pragma_database_list WHERE name = 'vectors';";
        return (long)(await command.ExecuteScalarAsync(cancellationToken))! > 0;
    }
}
