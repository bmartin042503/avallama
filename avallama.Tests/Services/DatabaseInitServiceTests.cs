using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using avallama.Services;
using avallama.Tests.Extensions;
using Xunit;

namespace avallama.Tests.Services;

public class DatabaseInitServiceTests
{

    [Fact]
    public async Task InitializeSchemaAsync_DoesNotThrow()
    {
        var service = new DatabaseInitService(isTest: true);
        await AsyncAssertExtensions.DoesNotThrowAsync(async () =>
        {
            await using var conn = await service.GetOpenConnectionAsync();
            Assert.NotNull(conn);
            Assert.Equal(System.Data.ConnectionState.Open, conn.State);
        });
    }

    [Fact]
    public async Task InitializeSchemaAsync_CreatesShemaMetadata_WithCorrectHash()
    {
        var service = new DatabaseInitService();

        await using var conn = await service.GetOpenConnectionAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM schema_metadata WHERE key = 'schema_hash'";
        var hash = await cmd.ExecuteScalarAsync() as string;

        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.Equal(128, hash.Length);
    }

    [Fact]
    public async Task InitializeSchemaAsync_MessagesIndex_IsUsed_WhenQueryingMessages_ForConversation()
    {
        var service = new DatabaseInitService(isTest: true);
        await using var conn = await service.GetOpenConnectionAsync();

        await using (var insertCmd = conn.CreateCommand())
        {
            insertCmd.CommandText = """
                              INSERT INTO conversations (id, title, created_at, last_message_sent_at)
                              VALUES ('conv-1', 'Test', '2024-01-01T00:00:00Z', '2024-01-01T01:00:00Z');

                              INSERT INTO messages (conversation_id, role, message, timestamp)
                              VALUES ('conv-1', 'user', 'Test', '2024-01-01T00:00:00Z');
                              """;
            await insertCmd.ExecuteNonQueryAsync();
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "EXPLAIN QUERY PLAN SELECT * FROM messages WHERE conversation_id = 'conv-1' ORDER BY timestamp";
        await using var reader = await cmd.ExecuteReaderAsync();

        var queryPlan = new List<string>();
        while (await reader.ReadAsync())
        {
            queryPlan.Add(reader.GetString(3));
        }

        var planText = string.Join(" ", queryPlan);
        Assert.Contains("idx_messages_convo_timestamp", planText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InitializeSchemaAsync_ConversationsIndex_IsUsed_WhenQueryingByLastMessage()
    {
        var service = new DatabaseInitService(isTest: true);
        await using var conn = await service.GetOpenConnectionAsync();

        await using (var insertCmd = conn.CreateCommand())
        {
            insertCmd.CommandText = """
                                    INSERT INTO conversations (id, title, created_at, last_message_sent_at)
                                    VALUES ('conv-1', 'Test', '2024-01-01T00:00:00Z', '2024-01-01T01:00:00Z');
                                    """;
            await insertCmd.ExecuteNonQueryAsync();
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "EXPLAIN QUERY PLAN SELECT * FROM conversations WHERE last_message_sent_at IS NOT NULL ORDER BY last_message_sent_at DESC";
        await using var reader = await cmd.ExecuteReaderAsync();

        var queryPlan = new List<string>();
        while (await reader.ReadAsync())
        {
            queryPlan.Add(reader.GetString(3));
        }

        var planText = string.Join(" ", queryPlan);
        Assert.Contains("idx_conversations_last_msg", planText, StringComparison.OrdinalIgnoreCase);
    }
}
