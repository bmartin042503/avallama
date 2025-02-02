namespace avallama.Models;

public class GeneratedMessage(string content) : Message(content)
{
    public int GenerationSpeed { get; set; } // token/sec 
}