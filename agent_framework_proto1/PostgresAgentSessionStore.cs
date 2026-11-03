using System.Text.Json;
using Microsoft.Agents.AI;
using Npgsql;

namespace AgentFrameworkProto1;

/// <summary>
/// Persists entire AG-UI/MAF AgentSession objects in Postgres, keyed by the
/// protocol-supplied session id (AG-UI threadId). This is session-level
/// persistence (resumable threads across process restarts), distinct from
/// agent_framework_quickstart's ChatHistoryProvider, which persists
/// individual chat messages.
/// </summary>
public sealed class PostgresAgentSessionStore : AgentSessionStore
{
    private readonly string _connectionString;

    public PostgresAgentSessionStore(string connectionString)
    {
        _connectionString = connectionString;
        EnsureSchema();
    }

    public override async ValueTask SaveSessionAsync(
        AIAgent agent,
        AgentSessionStoreKey key,
        AgentSession session,
        CancellationToken cancellationToken = default)
    {
        var serialized = await agent.SerializeSessionAsync(session, jsonSerializerOptions: null, cancellationToken);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO agent_sessions (session_id, session_json, updated_at)
            VALUES (@session_id, @session_json, now())
            ON CONFLICT (session_id) DO UPDATE
                SET session_json = EXCLUDED.session_json, updated_at = now()
            """;
        command.Parameters.AddWithValue("session_id", key.SessionId);
        command.Parameters.AddWithValue("session_json", serialized.GetRawText());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public override async ValueTask<AgentSession?> GetSessionAsync(
        AIAgent agent,
        AgentSessionStoreKey key,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT session_json FROM agent_sessions WHERE session_id = @session_id";
        command.Parameters.AddWithValue("session_id", key.SessionId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var json = reader.GetString(0);
        using var document = JsonDocument.Parse(json);
        return await agent.DeserializeSessionAsync(document.RootElement.Clone(), jsonSerializerOptions: null, cancellationToken);
    }

    public override async ValueTask DeleteSessionAsync(
        AIAgent agent,
        AgentSessionStoreKey key,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM agent_sessions WHERE session_id = @session_id";
        command.Parameters.AddWithValue("session_id", key.SessionId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private void EnsureSchema()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS agent_sessions (
                session_id TEXT PRIMARY KEY,
                session_json TEXT NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
            );
            """;
        command.ExecuteNonQuery();
    }
}
