namespace avallama.Models;

public class GeneratedMessage(string content) : Message(content)
{
    public double GenerationSpeed { get; set; } // token/sec 
}