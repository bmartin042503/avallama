using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using avallama.Models;
using avallama.Utilities;
using Microsoft.Data.Sqlite;

namespace avallama.Services;

public interface IDatabaseService
{
    public Task<Guid> CreateConversation(Conversation conversation);
    public Task InsertMessage(Guid conversationId, Message message, string? modelName, double? tokenPerSec);
    public Task<ObservableStack<Conversation>> GetConversations();
    public Task<bool> UpdateConversationTitle(Conversation conversation);
    public Task<ObservableCollection<Message>> GetMessagesForConversation(Conversation conversation);

}

public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;
    private readonly DialogService _dialogService;

    public DatabaseService(DialogService dialogService)
    {
        var exeDir = AppContext.BaseDirectory;
        var dbPath = Path.Combine(exeDir, "avallama.db");
        _connectionString = $"Data Source={dbPath}";
        _dialogService = dialogService;
    }

    public async Task<Guid> CreateConversation(Conversation conversation)
    {
        var conversationId = Guid.NewGuid();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO conversations (id, title, created_at, last_message_sent_at) VALUES (@Id, @Title, @CreatedAt, @LastMessageSentAt)";
        cmd.Parameters.AddWithValue("@Id", conversationId);
        cmd.Parameters.AddWithValue("@Title", conversation.Title);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
        cmd.Parameters.AddWithValue("@LastMessageSentAt", DateTime.Now);

        try
        {
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception e)
        {
            _dialogService.ShowErrorDialog(LocalizationService.GetString("DATABASE_ERROR_OCCURRED") + e.Message);
            throw;
        }
        
        return conversationId;
    }

    public async Task InsertMessage(Guid conversationId, Message message, string? modelName, double? tokenPerSec)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText =
            "INSERT INTO messages (conversation_id, role, message, timestamp, model_name, tokens_per_sec) VALUES (@ConversationId, @Role, @Message, @Timestamp, @ModelName, @TokenPerSec)";
        cmd.Parameters.AddWithValue("@ConversationId", conversationId);
        cmd.Parameters.AddWithValue("@Role", message is GeneratedMessage ? "assistant" : "user");
        cmd.Parameters.AddWithValue("@Message", message.Content);
        cmd.Parameters.AddWithValue("@Timestamp", DateTime.Now);
        cmd.Parameters.AddWithValue("@ModelName", modelName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@TokenPerSec", tokenPerSec ?? (object)DBNull.Value);

        if (message is not FailedMessage)
        {
            try
            {
                await cmd.ExecuteNonQueryAsync();
                await UpdateLastMessageSent(conversationId);
            }
            catch (Exception e)
            {
                _dialogService.ShowErrorDialog(LocalizationService.GetString("DATABASE_ERROR_OCCURRED") + e.Message);
                throw;
                // TODO Implement logging service
            }
        }
    }

    private async Task UpdateLastMessageSent(Guid conversationId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE conversations SET last_message_sent_at = @LastMessageSentAt WHERE id = @ConversationId";
        cmd.Parameters.AddWithValue("@LastMessageSentAt", DateTime.Now);
        cmd.Parameters.AddWithValue("@ConversationId", conversationId);
        try
        {
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception e)
        {
            _dialogService.ShowErrorDialog(LocalizationService.GetString("DATABASE_ERROR_OCCURRED") + e.Message);
            throw;
        }
    }

    public async Task<ObservableStack<Conversation>> GetConversations()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM conversations ORDER BY last_message_sent_at DESC";
        await using var reader = await cmd.ExecuteReaderAsync();
        var conversations = new ObservableStack<Conversation>();
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
        catch (Exception e)
        {
            _dialogService.ShowErrorDialog(LocalizationService.GetString("DATABASE_ERROR_OCCURRED") + e.Message);
            throw;
        }
        
        var lastModels = await GetLastModelsForConversationsAsync(conversationIds);
        
        foreach (var conv in conversations)
        {
            conv.Model = lastModels.GetValueOrDefault(conv.ConversationId, "llama3.2:2b"); // this default could be set in settings?
        }
        
        return conversations;
    }

    private async Task<Dictionary<Guid, string>> GetLastModelsForConversationsAsync(IEnumerable<Guid> conversationIds)
    {
        var lastModels = new Dictionary<Guid, string>();

        var enumerable = conversationIds as Guid[] ?? conversationIds.ToArray();
        if (enumerable.Length == 0)
            return lastModels;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var idsParam = string.Join(",", enumerable.Select((_, i) => $"@id{i}"));
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
                                   SELECT conversation_id, model_name
                                   FROM messages
                                   WHERE conversation_id IN ({idsParam})
                                   AND timestamp = (
                                       SELECT MAX(timestamp) 
                                       FROM messages m2 
                                       WHERE m2.conversation_id = messages.conversation_id
                                   )
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
        catch (Exception e)
        {
            _dialogService.ShowErrorDialog(
                LocalizationService.GetString("DATABASE_ERROR_OCCURRED") + e.Message);
            throw;
        }

        return lastModels;
    }


    public async Task<bool> UpdateConversationTitle(Conversation conversation)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE conversations SET title = @title WHERE id = @ConversationId";
        cmd.Parameters.AddWithValue("@title", conversation.Title);
        cmd.Parameters.AddWithValue("@ConversationId", conversation.ConversationId.ToString().ToUpper());
        try
        {
            var res = await cmd.ExecuteNonQueryAsync();
            return res > 0;
        }
        catch (Exception e)
        {
            _dialogService.ShowErrorDialog(LocalizationService.GetString("DATABASE_ERROR_OCCURRED") + e.Message);
            throw;
        }
    }

    public async Task<ObservableCollection<Message>> GetMessagesForConversation(Conversation conversation)
    {
        var messages = new ObservableCollection<Message>();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM messages WHERE conversation_id = @ConversationId ORDER BY timestamp";
        cmd.Parameters.AddWithValue("@ConversationId", conversation.ConversationId.ToString().ToUpper());
        await using var reader = await cmd.ExecuteReaderAsync();
        try
        {
            while (await reader.ReadAsync())
            {
                Message message;
                switch (reader["role"].ToString())
                {
                    case "user":
                        message = new Message(reader["message"].ToString()!);
                        messages.Add(message);
                        break;
                    case "assistant":
                        message = new GeneratedMessage(reader["message"].ToString()!, (double)reader["tokens_per_sec"]);
                        messages.Add(message);
                        break;
                }
            }
        }
        catch (Exception e)
        {
            _dialogService.ShowErrorDialog(LocalizationService.GetString("DATABASE_ERROR_OCCURRED") + e.Message);
            throw;
        }
        return messages;
    }
}