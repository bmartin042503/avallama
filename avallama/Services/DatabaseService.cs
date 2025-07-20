using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace avallama.Services;

public interface IDatabaseService
{
    //TODO i guess
}

public class DatabaseService : IDatabaseService
{
    private readonly string _dbPath;

    public DatabaseService()
    {
        var exeDir = AppContext.BaseDirectory;
        _dbPath = Path.Combine(exeDir, "avallama.db");
    }

    public async Task<Guid> CreateConversation()
    {
        var conversationId = Guid.NewGuid();

        await using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();
        
        var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO conversations (id, title, created_at, last_message_sent_at) VALUES (@Id, @Title, @CreatedAt, @LastMessageSentAt)";
        cmd.Parameters.AddWithValue("@Id", conversationId);
        cmd.Parameters.AddWithValue("@Title", LocalizationService.GetString("NEW_CONVERSATION"));
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
        cmd.Parameters.AddWithValue("@LastMessageSentAt", DateTime.Now);
        
        await cmd.ExecuteNonQueryAsync();
        return conversationId;
    }
}