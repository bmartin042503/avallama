using System;
using System.Collections.Generic;

namespace avallama.Models;

public class OllamaModelFamily(
    string name,
    string description,
    long pullCount,
    IList<string> labels,
    int tagCount,
    DateTime lastUpdated)
{
    public string Name { get; set; } = name;
    public string Description { get; set; } = description;
    public long PullCount { get; set; } = pullCount;
    public IList<string> Labels { get; set; } = labels;
    public int TagCount { get; set; } = tagCount;
    public DateTime LastUpdated { get; set; } = lastUpdated;
}
