// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;

namespace avallama.Models.Ollama;

public class OllamaModelFamily
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long PullCount { get; set; }
    public IList<string> Labels { get; set; } = new List<string>();
    public int TagCount { get; set; }
    public DateTime LastUpdated { get; set; }
}
