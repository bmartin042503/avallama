using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace avallama.Services;

public interface IDatabaseInitService
{
    Task<SqliteConnection> GetOpenConnectionAsync();
}

public class DatabaseInitService : IDatabaseInitService
{
    private readonly string _dbPath;

    public DatabaseInitService()
    {
        var exeDir = AppContext.BaseDirectory;
        _dbPath = Path.Combine(exeDir, "avallama.db");
    }

    public async Task<SqliteConnection> GetOpenConnectionAsync()
    {
        var isNew = !File.Exists(_dbPath);

        var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        if (isNew) await InitializeSchemaAsync(connection);

        var isValid = await IsDatabaseValidAsync(connection);
        var hasRequiredTables = await DoesTableExistAsync(connection, "conversations") &&
                                await DoesTableExistAsync(connection, "messages");

        if (!isValid || !hasRequiredTables)
        {
            // Ezt a hibat esetleg egy debug logba vagy valahogy mashogy meg lehetne jeleniteni
            Console.WriteLine(
                "Database file is present but either corrupted or missing required tables. Reinitializing.");
            await InitializeSchemaAsync(connection);
        }

        return connection;
    }

    private async Task InitializeSchemaAsync(SqliteConnection conn)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
                          PRAGMA foreign_keys = ON;

                          CREATE TABLE IF NOT EXISTS conversations (
                          id TEXT PRIMARY KEY,
                          title TEXT NOT NULL,
                          created_at TEXT NOT NULL,
                          last_message_sent_at TEXT
                          );

                          CREATE TABLE IF NOT EXISTS messages (
                          id INTEGER PRIMARY KEY AUTOINCREMENT,
                          conversation_id TEXT NOT NULL,
                          role TEXT NOT NULL,
                          message TEXT NOT NULL,
                          timestamp TEXT NOT NULL,
                          model_name TEXT,
                          tokens_per_sec REAL,
                          FOREIGN KEY (conversation_id) REFERENCES conversations(id) ON DELETE CASCADE
                          );

                          CREATE INDEX IF NOT EXISTS idx_messages_convo_timestamp
                          ON messages (conversation_id, timestamp);

                          CREATE INDEX IF NOT EXISTS idx_conversations_last_msg
                          ON conversations (last_message_sent_at);
                          """;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<bool> IsDatabaseValidAsync(SqliteConnection conn)
    {
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            var result = (string?)await cmd.ExecuteScalarAsync();

            return result == "ok";
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static async Task<bool> DoesTableExistAsync(SqliteConnection connection, string tableName)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@name;";
        cmd.Parameters.AddWithValue("@name", tableName);
        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }
}