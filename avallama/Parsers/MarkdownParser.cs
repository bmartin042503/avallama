using System.Collections.Generic;
using System.Text.RegularExpressions;
using avallama.Constants;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;

namespace avallama.Parsers;

public static class MarkdownParser
{
    private static double H1FontSize = 22.0;
    private static double H2FontSize = 18.0;
    private static double H3FontSize = 14.0;
    private static double H4FontSize = 10.0;
    private static double H5FontSize = 6.0;

    private static readonly Dictionary<MarkdownType, string> Patterns = new()
    {
        { MarkdownType.H1, @"^# (.+)$" },
        { MarkdownType.H2, @"^## (.+)$" },
        { MarkdownType.H3, @"^### (.+)$" },
        { MarkdownType.H4, @"^#### (.+)$" },
        { MarkdownType.H5, @"^##### (.+)$" },
        { MarkdownType.Bold, @"\*\*(.+?)\*\*" },
        { MarkdownType.Italic, @"\*(.+?)\*" },
        { MarkdownType.Blockquote, @"^> (.+)$" },
        { MarkdownType.OrderedItem, @"^\d+\. (.+)$" },
        { MarkdownType.UnorderedItem, @"^- (.+)$" },
        { MarkdownType.InlineCode, @"`(.+?)`" },
        { MarkdownType.HorizontalRule, @"^\-{3,}$" },
        { MarkdownType.Link, @"\[(.+?)\]\((.+?)\)" },
        { MarkdownType.CodeBlock, @"```([\s\S]+?)```" }
    };

    public static List<MarkdownStyleProperties> TextToMarkdownStyleProperties(string text, FontFamily fontFamily)
    {
        var result = new List<MarkdownStyleProperties>();

        foreach (var pattern in Patterns)
        {
            var regex = new Regex(pattern.Value, RegexOptions.Multiline);
            var matches = regex.Matches(text);

            foreach (Match match in matches)
            {
                var properties = GetMarkdownStyleProperties(pattern.Key, match.Value);
                properties.Start = match.Index;
                properties.Length = match.Length;
                
                result.Add(properties);
            }
        }

        return result;
    }

    private static MarkdownStyleProperties GetMarkdownStyleProperties(MarkdownType markdownType, string content)
    {
        MarkdownStyleProperties properties = new()
        {
            FontFamily = "Default",
            FontSize = 0.0,
            FontWeight = FontWeight.Normal,
            FontStyle = FontStyle.Normal,
            MarkdownType = markdownType,
            Content = content
        };

        switch (markdownType)
        {
            case MarkdownType.H1:
                properties.FontSize = H1FontSize;
                properties.FontWeight = FontWeight.Bold;
                break;
            case MarkdownType.H2:
                properties.FontSize = H2FontSize;
                properties.FontWeight = FontWeight.Bold;
                break;
            case MarkdownType.H3:
                properties.FontSize = H3FontSize;
                properties.FontWeight = FontWeight.Bold;
                break;
            case MarkdownType.H4:
                properties.FontSize = H4FontSize;
                properties.FontWeight = FontWeight.Bold;
                break;
            case MarkdownType.H5:
                properties.FontSize = H5FontSize;
                properties.FontWeight = FontWeight.Bold;
                break;
            case MarkdownType.Bold:
                properties.FontWeight = FontWeight.Bold;
                break;
            case MarkdownType.Italic:
                properties.FontStyle = FontStyle.Italic;
                break;
            case MarkdownType.InlineCode:
            case MarkdownType.CodeBlock:
                properties.FontFamily = "Courier New";
                break;
            case MarkdownType.Blockquote:
            case MarkdownType.Link:
                // egyelőre semmi
                break;
        }
        return properties;
    }

    public static string RemoveMarkdownFormatSyntax(string text)
    {
        string cleanedText = text;

        foreach (var pattern in Patterns)
        {
            cleanedText = Regex.Replace(cleanedText, pattern.Value, match =>
            {
                switch (pattern.Key)
                {
                    case MarkdownType.H1:
                    case MarkdownType.H2:
                    case MarkdownType.H3:
                    case MarkdownType.H4:
                    case MarkdownType.H5:
                    case MarkdownType.Bold:
                    case MarkdownType.Italic:
                    case MarkdownType.InlineCode:
                    case MarkdownType.CodeBlock:
                    case MarkdownType.Blockquote:
                    case MarkdownType.Link:
                        // az első index kell, mert a 0-ban mást tárol, pl. ha nem talál matchet stb.
                        return match.Groups[1].Value;
                    case MarkdownType.HorizontalRule:
                        return "";
                    default:
                        return match.Value;
                }
            }, RegexOptions.Multiline);
        }

        return cleanedText;
    }
}