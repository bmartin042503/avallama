using System;
using System.Collections.Generic;

namespace avallama.Parsers;

public enum MarkdownType
{
    Paragraph,
    Heading,
    Link,
    CodeBlock,
    Italic,
    ItalicBold,
    Bold,
    StrikeThrough,
    Blockquote,
    InlineCode,
    HorizontalRule,
    
}

public class MarkdownElement
{
    public required string Content { get; set; }
    public MarkdownType Type { get; set; }
    public MarkdownElement? Parent { get; set; }
    public IEnumerable<MarkdownElement>? Children { get; set; }
}

public static class MarkdownParser
{
    // ígérem az idén még kész lesz XD
}