// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using avallama.Models;
using avallama.Utilities;
using Microsoft.Data.Sqlite;

namespace avallama.Services;

internal static class Roles
{
    public const string User = "user";
    public const string Assistant = "assistant";
}

public interface IDatabaseService
{
    public Task<Guid> CreateConversation(Conversation conversation);
    public Task InsertMessage(Guid conversationId, Message message, string? modelName, double? tokenPerSec);
    public Task<ObservableStack<Conversation>> GetConversations();
    public Task<bool> UpdateConversationTitle(Conversation conversation);
    public Task<ObservableCollection<Message>> GetMessagesForConversation(Conversation conversation);
}

public class DatabaseService : IDatabaseService, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public DatabaseService(SqliteConnection? testConnection = null)
    {
        if (testConnection is not null)
        {
            _connection = testConnection;
            _connection.Open();
            return;
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDir = Path.Combine(appData, "avallama");
        if (!Directory.Exists(appDir))
            Directory.CreateDirectory(appDir);

        var dbPath = Path.Combine(appDir, "avallama.db");
        var connectionString = $"Data Source={dbPath}";
        _connection = new SqliteConnection(connectionString);
        _connection.Open();
    }

    public async Task<Guid> CreateConversation(Conversation conversation)
    {
        var conversationId = Guid.NewGuid();

        await _semaphore.WaitAsync();
        try
        {
            await using var cmd = _connection.CreateCommand();
            cmd.CommandText =
                "INSERT INTO conversations (id, title, created_at, last_message_sent_at) VALUES (@Id, @Title, @CreatedAt, @LastMessageSentAt)";
            cmd.Parameters.AddWithValue("@Id", conversationId);
            cmd.Parameters.AddWithValue("@Title", conversation.Title);
            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
            cmd.Parameters.AddWithValue("@LastMessageSentAt", DateTime.Now);

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception)
            {
                // TODO: InterruptService
                throw;
            }
        }
        finally
        {
            _semaphore.Release();
        }

        return conversationId;
    }

    public async Task InsertMessage(Guid conversationId, Message message, string? modelName, double? tokenPerSec)
    {
        if (message is FailedMessage) return;

        await _semaphore.WaitAsync();
        try
        {
            await using var transaction = await _connection.BeginTransactionAsync();
            var now = DateTime.Now;

            try
            {
                await using var insertCmd = _connection.CreateCommand();
                // Create transaction to ensure both inserts succeed or fail together
                insertCmd.Transaction = (SqliteTransaction)transaction;

                // Insert message
                insertCmd.CommandText =
                    "INSERT INTO messages (conversation_id, role, message, timestamp, model_name, tokens_per_sec) VALUES (@ConversationId, @Role, @Message, @Timestamp, @ModelName, @TokenPerSec)";
                insertCmd.Parameters.AddWithValue("@ConversationId", conversationId);
                insertCmd.Parameters.AddWithValue("@Role", message is GeneratedMessage ? "assistant" : "user");
                insertCmd.Parameters.AddWithValue("@Message", message.Content);
                insertCmd.Parameters.AddWithValue("@Timestamp", now);
                insertCmd.Parameters.AddWithValue("@ModelName", modelName ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("@TokenPerSec", tokenPerSec ?? (object)DBNull.Value);
                await insertCmd.ExecuteNonQueryAsync();

                // Update conversation's last_message_sent_at
                await using var updateCmd = _connection.CreateCommand();
                updateCmd.Transaction = (SqliteTransaction)transaction;
                updateCmd.CommandText =
                    "UPDATE conversations SET last_message_sent_at = @LastMessageSentAt WHERE id = @ConversationId";
                updateCmd.Parameters.AddWithValue("@LastMessageSentAt", now);
                updateCmd.Parameters.AddWithValue("@ConversationId", conversationId);
                await updateCmd.ExecuteNonQueryAsync();

                // Commit transaction
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                // TODO: InterruptService
                throw;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<ObservableStack<Conversation>> GetConversations()
    {
        ObservableStack<Conversation> conversations;
        await _semaphore.WaitAsync();
        try
        {
            await using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM conversations ORDER BY last_message_sent_at DESC";
            await using var reader = await cmd.ExecuteReaderAsync();
            conversations = [];
            var conversationIds = new List<Guid>();
            if (!reader.HasRows) return conversations;

            try
            {
                while (await reader.ReadAsync())
                {
                    var id = Guid.Parse((string)reader["id"]);
                    var conversation = new Conversation(
                        Guid.Parse((string)reader["id"]),
                        (string)reader["title"],
                        new List<Message>()
                    );
                    conversations.Add(conversation);
                    conversationIds.Add(id);
                }
            }
            catch (Exception)
            {
                // TODO: InterruptService
                throw;
            }

            var lastModels = await GetLastModelsForConversationsAsync(conversationIds);

            foreach (var conv in conversations)
            {
                conv.Model =
                    lastModels.GetValueOrDefault(conv.ConversationId,
                        "llama3.2:2b"); // this default could be set in settings?
            }

        }
        finally
        {
            _semaphore.Release();
        }

        return conversations;
    }

    private async Task<Dictionary<Guid, string>> GetLastModelsForConversationsAsync(IEnumerable<Guid> conversationIds)
    {
        // Semaphore is already held by caller

        var lastModels = new Dictionary<Guid, string>();

        var enumerable = conversationIds as Guid[] ?? conversationIds.ToArray();
        if (enumerable.Length == 0)
            return lastModels;

        var idsParam = string.Join(",", enumerable.Select((_, i) => $"@id{i}"));
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"""
                               SELECT conversation_id, model_name
                               FROM (
                                   SELECT conversation_id, model_name,
                                          ROW_NUMBER() OVER (PARTITION BY conversation_id ORDER BY timestamp DESC) AS rn
                                   FROM messages
                                   WHERE conversation_id IN ({idsParam})
                               )
                               WHERE rn = 1
                           """;


        var index = 0;
        foreach (var id in enumerable)
            cmd.Parameters.AddWithValue($"@id{index++}", id.ToString());

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var convId = Guid.Parse((string)reader["conversation_id"]);
                var model = reader.IsDBNull(1) ? "llama3.2:2b" : (string)reader["model_name"];
                lastModels[convId] = model;
            }
        }
        catch (Exception)
        {
            // TODO: InterruptService
            throw;
        }

        return lastModels;
    }


    public async Task<bool> UpdateConversationTitle(Conversation conversation)
    {
        await _semaphore.WaitAsync();
        try
        {
            await using var cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE conversations SET title = @title WHERE id = @ConversationId";
            cmd.Parameters.AddWithValue("@title", conversation.Title);
            cmd.Parameters.AddWithValue("@ConversationId", conversation.ConversationId);
            try
            {
                var res = await cmd.ExecuteNonQueryAsync();
                return res > 0;
            }
            catch (Exception)
            {
                // TODO: InterruptService
                throw;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<ObservableCollection<Message>> GetMessagesForConversation(Conversation conversation)
    {
        ObservableCollection<Message> messages;
        await _semaphore.WaitAsync();
        try
        {
            messages = [];
            await using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM messages WHERE conversation_id = @ConversationId ORDER BY timestamp";
            cmd.Parameters.AddWithValue("@ConversationId", conversation.ConversationId);
            await using var reader = await cmd.ExecuteReaderAsync();
            try
            {
                while (await reader.ReadAsync())
                {
                    var role = reader["role"].ToString();
                    var content = reader["message"].ToString()!;

                    var message = role switch
                    {
                        Roles.User => new Message(content),
                        Roles.Assistant => new GeneratedMessage(content, (double)reader["tokens_per_sec"]),
                        _ => throw new InvalidOperationException($"Unknown role: {role}")
                    };

                    messages.Add(message);
                }
            }
            catch (Exception)
            {
                // TODO: InterruptService
                throw;
            }
        }
        finally
        {
            _semaphore.Release();
        }

        return messages;
    }

    public void Dispose()
    {
        _semaphore.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
