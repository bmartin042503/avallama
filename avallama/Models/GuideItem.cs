// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

namespace avallama.Models;

public class GuideItem(string title, string imageSource, string? description = null)
{
    public string Title { get; set; } = title;
    public string? Description { get; set; } = description;
    public string ImageSource { get; set; } = imageSource;
}