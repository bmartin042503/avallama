// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;

namespace avallama.Models.Ollama;

/// <summary>
/// Represents a family of Ollama models (e.g. 'llama3').
/// </summary>
public class OllamaModelFamily
{
    /// <summary>
    /// Gets or sets the name of the model family.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the model family.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total pull count for this model family.
    /// </summary>
    public long PullCount { get; set; }

    /// <summary>
    /// Gets or sets the list of labels associated with the family.
    /// </summary>
    public IList<string> Labels { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the number of tags (actual models) available in this family.
    /// </summary>
    public int TagCount { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the family was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; }
}
