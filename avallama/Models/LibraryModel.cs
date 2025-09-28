using System.Collections.Generic;

namespace avallama.Models;

public class LibraryModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long PullCount { get; set; }
    public IList<string> PopularTags { get; set; } = new List<string>();
    public int TagCount { get; set; }
    public string LastUpdated { get; set; } = string.Empty;
}
