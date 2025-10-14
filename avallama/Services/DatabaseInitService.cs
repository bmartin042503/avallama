// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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

    // Canonical database schema, changing this will trigger a schema migration
    // This is not able to handle complex migrations, which would probably facilitate an "API breaking" change anyway
    private const string CanonicalSchema = """
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

    public DatabaseInitService(bool? isTest = false)
    {
        if (isTest.HasValue && isTest.Value)
        {
            _dbPath = ":memory:";
            return;
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDir = Path.Combine(appData, "avallama");
        if (!Directory.Exists(appDir))
            Directory.CreateDirectory(appDir);

        _dbPath = Path.Combine(appDir, "avallama.db");
    }

    public async Task<SqliteConnection> GetOpenConnectionAsync()
    {
        var isNew = !File.Exists(_dbPath);

        var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        if (isNew) await InitializeSchemaAsync(connection);

        var hasRequiredTables = await HasRequiredTablesAsync(connection);

        if (!hasRequiredTables)
        {
            //TODO: LoggingService
            Console.WriteLine("Database file is missing required tables. Reinitializing schema.");
            await InitializeSchemaAsync(connection);
            return connection;
        }

        var needsMigration = await NeedsMigrationAsync(connection);

        if (!needsMigration) return connection;
        Console.WriteLine("Database schema is outdated. Migrating...");
        await MigrateSchemaAsync(connection);
        Console.WriteLine("Database migration completed.");

        return connection;
    }

    private static async Task InitializeSchemaAsync(SqliteConnection conn)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
                          PRAGMA foreign_keys = ON;
                          PRAGMA journal_mode = WAL;
                          PRAGMA synchronous = NORMAL;
                          PRAGMA temp_store = MEMORY;
                          PRAGMA cache_size = 32000;

                          CREATE TABLE IF NOT EXISTS schema_metadata (
                          key TEXT PRIMARY KEY,
                          value TEXT NOT NULL
                          );

                          {CanonicalSchema}
                          """;
        await cmd.ExecuteNonQueryAsync();
        await StoreSchemaHashAsync(conn);
    }

    private static async Task<bool> NeedsMigrationAsync(SqliteConnection conn)
    {
        await using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='schema_metadata'";
        var exists = await checkCmd.ExecuteScalarAsync();
        if (exists is null) return true;

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM schema_metadata WHERE key = 'schema_hash' LIMIT 1";
        var storedHash = await cmd.ExecuteScalarAsync() as string;
        var currentHash = ComputeSchemaHash();
        return storedHash != currentHash;
    }

    private static async Task MigrateSchemaAsync(SqliteConnection conn)
    {
        await using var transaction = await conn.BeginTransactionAsync();
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                              CREATE TABLE IF NOT EXISTS schema_metadata (
                              key TEXT PRIMARY KEY,
                              value TEXT NOT NULL
                              );
                              """;
            await cmd.ExecuteNonQueryAsync();

            cmd.CommandText = CanonicalSchema;
            await cmd.ExecuteNonQueryAsync();

            await StoreSchemaHashAsync(conn);

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException("Schema migration failed.", ex);
        }

    }

    private static async Task StoreSchemaHashAsync(SqliteConnection conn)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO schema_metadata (key, value) VALUES ('schema_hash', @Hash)";
        cmd.Parameters.AddWithValue("@Hash", ComputeSchemaHash());
        await cmd.ExecuteNonQueryAsync();
    }

    private static string ComputeSchemaHash()
    {
        var normalized = NormalizeSchema(CanonicalSchema);
        var hashBytes = SHA512.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hashBytes);
    }

    private static string NormalizeSchema(string schema)
    {
        return string.Join(" ", schema.Split([' ', '\r', '\n', '\t'],
            StringSplitOptions.RemoveEmptyEntries));
    }

    private static async Task<bool> HasRequiredTablesAsync(SqliteConnection connection)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
                          SELECT COUNT(*) FROM sqlite_master
                          WHERE type='table' AND name IN ('conversations', 'messages');
                          """;
        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
        return count == 2;
    }
}
