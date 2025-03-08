// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;

namespace avallama.Parsers;

public enum MarkdownSyntax
{
    Default, // paragraph
    Heading, // # ## ### #### #####
    Asterisk, // * ** ***
    Strikethrough, // ~~
    CodeSpecifier, // ` ```
    OrderedSpecifier, // 1. 
    UnorderedSpecifier, // - 
    Blockquote, // > >> >>> ...
    HorizontalRule, // ---
    Link // []()
}

public class MarkdownElement
{
    public string? Content { get; set; }
    public MarkdownSyntax Syntax { get; set; }
    public MarkdownElement? Parent { get; set; }
    public IEnumerable<MarkdownElement>? Children { get; set; }
    public int Weight { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
}

public static class MarkdownParser
{
    public static IEnumerable<MarkdownElement> Parse(string text)
    {
        List<MarkdownElement> elements = [];
        // TODO: markdown lexer
        return elements;
    }
    
    
    
}