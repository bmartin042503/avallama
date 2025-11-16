using System;
using System.Collections.Generic;

namespace avallama.Models;

public class OllamaModelFamily
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long PullCount { get; set; }
    public IList<string> Labels { get; set; } = new List<string>();
    public int TagCount { get; set; }
    public DateTime LastUpdated { get; set; }
}
