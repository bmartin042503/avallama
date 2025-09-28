namespace avallama.Dtos;

public sealed class OllamaShowResponse
{
    public string? Parameters { get; set; }
    public long Size { get; set; }
    public OllamaDetails? Details { get; set; }

    public sealed class OllamaDetails
    {
        public string? Format { get; set; }
        public string? Quantization { get; set; }
        public string? Family { get; set; }
        public int? ContextLength { get; set; }
    }
}
