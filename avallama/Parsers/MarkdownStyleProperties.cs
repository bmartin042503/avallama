using System;
using avallama.Constants;
using Avalonia.Media;

namespace avallama.Parsers;

public class MarkdownStyleProperties
{
    private string _fontFamily;
    private double _fontSize;
    private FontWeight _fontWeight;
    private FontStyle _fontStyle;
    private MarkdownType _markdownType;
    private int _start;
    private int _length;

    public string FontFamily
    {
        get => _fontFamily;
        set => _fontFamily = value;
    }

    public FontStyle FontStyle
    {
        get => _fontStyle;
        set => _fontStyle = value;
    }

    public double FontSize
    {
        get => _fontSize;
        set => _fontSize = value;
    }

    public FontWeight FontWeight
    {
        get => _fontWeight;
        set => _fontWeight = value;
    }

    public MarkdownType MarkdownType
    {
        get => _markdownType;
        set => _markdownType = value;
    }

    public int Start
    {
        get => _start;
        set => _start = value;
    }

    public int Length
    {
        get => _length;
        set => _length = value;
    }
}