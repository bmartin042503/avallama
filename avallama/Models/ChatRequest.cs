using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace avallama.Models;

public class ChatRequest
{
    //igazabol itt lehetne dinamikusan beallitani a modellt majd
    [JsonPropertyName("model")]
    public string Model { get; set; } = "llama3.2";

    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; }

    public ChatRequest(List<Message> messages)
    {
        Messages = ConvertToChatMessages(messages);
    }

    private static List<ChatMessage> ConvertToChatMessages(List<Message> messages)
    {
        var chatMessages = new List<ChatMessage>();

        foreach (var message in messages)
        {
            chatMessages.Add(new ChatMessage
            {
                Role = message is GeneratedMessage ? "assistant" : "user",
                Content = message.Content
            });
        }

        return chatMessages;
    }

    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(this, options);
    }
}

public class ChatMessage
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}