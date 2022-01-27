﻿using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

using BeUtl.Graphics;

namespace BeUtl.Media.TextFormatting;

public readonly record struct StringSpan(string Source, int Start, int Length)
{
    public static StringSpan Empty => new(string.Empty, 0, 0);

    public override string ToString()
    {
        return AsSpan().ToString();
    }

    public ReadOnlySpan<char> AsSpan()
    {
        return IsValid() ? Source.AsSpan(Start, Length) : default;
    }

    public StringSpan Slice(int start, int length)
    {
        var r = new StringSpan(Source, Start + start, length);
        return r;
    }

    public StringSpan Slice(int start)
    {
        var r = new StringSpan(Source, Start + start, Length - start);
        return r;
    }

    public bool IsValid()
    {
        return Start >= 0 && (Start + Length) <= Source.Length;
    }
}

public struct FormattedTextParser
{
    private readonly string _s;
    private int _lineCount = 0;

    public FormattedTextParser(string s)
    {
        _s = s;
    }

    public List<TextLine> ToLines(FormattedTextInfo defaultProps)
    {
        List<Token> tokens = Tokenize();
        Span<Token> spanTokens = CollectionsMarshal.AsSpan(tokens);
        var lines = new List<TextLine>(_lineCount);

        // 行を追加
        foreach (Token token in spanTokens)
        {
            if (token.Type == TokenType.NewLine)
            {
                lines.Add(new TextLine());
            }
        }

        int lineNum = 0;
        var font = new Stack<FontFamily>();
        var fontWeight = new Stack<FontWeight>();
        var fontStyle = new Stack<FontStyle>();
        var size = new Stack<float>();
        var color = new Stack<Color>();
        var space = new Stack<float>();
        var margin = new Stack<Thickness>();
        FontFamily curFont = defaultProps.Typeface.FontFamily;
        FontWeight curWeight = defaultProps.Typeface.Weight;
        FontStyle curStyle = defaultProps.Typeface.Style;
        float curSize = defaultProps.Size;
        Color curColor = defaultProps.Color;
        float curSpace = defaultProps.Space;
        Thickness curMargin = defaultProps.Margin;
        bool noParse = false;

        foreach (Token token in spanTokens)
        {
            if (!noParse && token.Type == TokenType.TagStart &&
                TryParseTag(token.Text, out TagInfo tag))
            {
                // 開始タグ
                if (tag.TryGetFont(out FontFamily font1))
                {
                    font.Push(curFont);
                    curFont = font1;
                }
                else if (tag.TryGetSize(out float size1))
                {
                    size.Push(curSize);
                    curSize = size1;
                }
                else if (tag.TryGetColor(out Color color1))
                {
                    color.Push(curColor);
                    curColor = color1;
                }
                else if (tag.TryGetCharSpace(out float space1))
                {
                    space.Push(curSpace);
                    curSpace = space1;
                }
                else if (tag.TryGetMargin(out Thickness margin1))
                {
                    margin.Push(curMargin);
                    curMargin = margin1;
                }
                else if (tag.TryGetFontStyle(out FontStyle fontStyle1))
                {
                    fontStyle.Push(curStyle);
                    curStyle = fontStyle1;
                }
                else if (tag.TryGetFontWeight(out FontWeight fontWeight1))
                {
                    fontWeight.Push(curWeight);
                    curWeight = fontWeight1;
                }
                else if (tag.Type == TagType.NoParse)
                {
                    noParse = true;
                }
                else
                {
                    throw new Exception($"{tag.Value} is invalid tag.");
                }

                continue;
            }
            else if (token.Type == TokenType.TagClose)
            {
                TagType closeTagType = GetCloseTagType(token.Text);
                if (closeTagType == TagType.NoParse)
                {
                    noParse = false;
                }

                if (!noParse)
                {
                    switch (closeTagType)
                    {
                        case TagType.Invalid:
                            goto default;
                        case TagType.Font:
                            curFont = font.PopOrDefault(defaultProps.Typeface.FontFamily);
                            break;
                        case TagType.Size:
                            curSize = size.PopOrDefault(defaultProps.Size);
                            break;
                        case TagType.Color:
                        case TagType.ColorHash:
                            curColor = color.PopOrDefault(defaultProps.Color);
                            break;
                        case TagType.CharSpace:
                            curSpace = space.PopOrDefault(defaultProps.Space);
                            break;
                        case TagType.Margin:
                            curMargin = margin.PopOrDefault(defaultProps.Margin);
                            break;
                        case TagType.FontWeightBold:
                        case TagType.FontWeight:
                            curWeight = fontWeight.PopOrDefault(defaultProps.Typeface.Weight);
                            break;
                        case TagType.FontStyle:
                        case TagType.FontStyleItalic:
                            curStyle = fontStyle.PopOrDefault(defaultProps.Typeface.Style);
                            break;
                        case TagType.NoParse:
                            noParse = false;
                            break;
                        default:
                            throw new Exception($"{token.Text} is invalid tag.");
                    }
                }
            }

            if (token.Type == TokenType.Content)
            {
                lines[lineNum].Elements.Add(new TextElement()
                {
                    Text = token.Text.AsSpan().ToString(),
                    Typeface = new Typeface(curFont, curStyle, curWeight),
                    Size = curSize,
                    Foreground = curColor.ToImmutableBrush(),
                    Spacing = curSpace,
                    Margin = curMargin,
                });
            }
            else if (token.Type == TokenType.NewLine)
            {
                lineNum++;
            }
            else if (noParse)
            {
                lines[lineNum].Elements.Add(new TextElement()
                {
                    Text = token.ToString(),
                    Typeface = new Typeface(curFont, curStyle, curWeight),
                    Size = curSize,
                    Foreground = curColor.ToImmutableBrush(),
                    Spacing = curSpace,
                    Margin = curMargin,
                });
            }
        }

        lines.RemoveAll(i =>
        {
            if (i.Elements.Count < 1)
            {
                i.Dispose();
                return true;
            }
            else
            {
                return false;
            }
        });

        return lines;
    }

    // '_s'を全てトークン化
    public List<Token> Tokenize()
    {
        _lineCount = 0;
        var result = new List<Token>();
        ReadOnlySpan<char> span = _s.AsSpan();

        foreach (ReadOnlySpan<char> linesp in span.EnumerateLines())
        {
            int start = span.IndexOf(linesp, StringComparison.Ordinal);
            int len = linesp.Length;

            Tokenize(new StringSpan(_s, start, len), result);

            result.Add(new Token(StringSpan.Empty, TokenType.NewLine));
            _lineCount++;
        }

        return result;
    }

    private void Process(StringSpan s, int start, int length, List<Token> result)
    {
        StringSpan prev = s.Slice(0, start);
        StringSpan tag = s.Slice(start, length);
        StringSpan next = s.Slice(start + length);

        if (prev.Length > 0 && prev.IsValid())
        {
            Tokenize(prev, result);
        }

        if (tag.Length > 0 && tag.IsValid())
        {
            TokenizeTag(tag, result);
        }

        if (next.Length > 0 && next.IsValid())
        {
            Tokenize(next, result);
        }
    }

    // 文字列をトークン化
    // Tokenize -> Process -> Tokenize
    //                     -> TokenizeTag
    //                     -> Tokenize
    private void Tokenize(StringSpan s, List<Token> result)
    {
        ReadOnlySpan<char> span = s.AsSpan();
        int tagStart = span.IndexOf("<", StringComparison.Ordinal);
        int tagEnd = span.IndexOf(">", StringComparison.Ordinal);

        bool isMatch = tagStart >= 0 && tagEnd >= 0 &&
            tagStart < tagEnd;

        if (isMatch)
        {
            Process(s, tagStart, tagEnd - tagStart + 1, result);
        }
        else if (s.Length > 0)
        {
            result.Add(new Token(s, TokenType.Content));
        }
    }

    // タグを解析
    private static bool TryParseTag(StringSpan tag, out TagInfo result)
    {
        ReadOnlySpan<char> span = tag.AsSpan();
        if (span.IsWhiteSpace())
        {
            result = default;
            return false;
        }

        int assignIdx = span.IndexOf("=", StringComparison.Ordinal);
        StringSpan first = tag;
        StringSpan second = StringSpan.Empty;
        if (assignIdx >= 0)
        {
            first = tag.Slice(0, assignIdx);
            second = tag.Slice(assignIdx + 1);
        }
        TagType tagType = GetTagType(first);

        // "<#fffff>" みたいなタグ
        if (tagType is TagType.ColorHash or TagType.FontWeightBold or TagType.FontStyleItalic or TagType.NoParse)
        {
            result = new TagInfo(first, tagType);
            return true;
        }
        else if (second.Length > 0)
        {
            result = new TagInfo(second, tagType);
            return true;
        }
        else
        {
            result = new TagInfo(tag, TagType.Invalid);
            return false;
        }
    }

    // タグをトークンにして追加
    private static void TokenizeTag(StringSpan s, List<Token> result)
    {
        // TagClose
        if (s.AsSpan().StartsWith("</", StringComparison.Ordinal))
        {
            result.Add(new Token(s, TokenType.TagClose));
        }
        else
        {
            result.Add(new Token(s, TokenType.TagStart));
        }
    }

    // 終了タグの種類を取得
    private static TagType GetCloseTagType(StringSpan tag)
    {
        ReadOnlySpan<char> span = tag.AsSpan();

        if (span.StartsWith("<", StringComparison.Ordinal))
        {
            span = span[1..];
        }
        if (span.EndsWith(">", StringComparison.Ordinal))
        {
            span = span[..^1];
        }

        if (span.SequenceEqual("/font"))
        {
            return TagType.Font;
        }
        else if (span.SequenceEqual("/size"))
        {
            return TagType.Size;
        }
        else if (span.SequenceEqual("/color"))
        {
            return TagType.Color;
        }
        else if (span.SequenceEqual("/cspace"))
        {
            return TagType.CharSpace;
        }
        else if (span.SequenceEqual("/margin"))
        {
            return TagType.Margin;
        }
        else if (span.SequenceEqual("/weight"))
        {
            return TagType.FontWeight;
        }
        else if (span.SequenceEqual("/style"))
        {
            return TagType.FontStyle;
        }
        else if (span.SequenceEqual("/b") || span.SequenceEqual("/bold"))
        {
            return TagType.FontWeightBold;
        }
        else if (span.SequenceEqual("/i") || span.SequenceEqual("/italic"))
        {
            return TagType.FontStyleItalic;
        }
        else if (span.SequenceEqual("/noparse"))
        {
            return TagType.NoParse;
        }
        else if (span.StartsWith("/#", StringComparison.Ordinal))
        {
            return TagType.ColorHash;
        }

        return TagType.Invalid;
    }

    // 開始タグの種類を取得
    private static TagType GetTagType(StringSpan tag)
    {
        ReadOnlySpan<char> span = tag.AsSpan();

        if (span.StartsWith("<", StringComparison.Ordinal))
        {
            span = span[1..];
        }
        if (span.EndsWith(">", StringComparison.Ordinal))
        {
            span = span[..^1];
        }

        if (span.SequenceEqual("font"))
        {
            return TagType.Font;
        }
        else if (span.SequenceEqual("size"))
        {
            return TagType.Size;
        }
        else if (span.SequenceEqual("color"))
        {
            return TagType.Color;
        }
        else if (span.SequenceEqual("cspace"))
        {
            return TagType.CharSpace;
        }
        else if (span.SequenceEqual("margin"))
        {
            return TagType.Margin;
        }
        else if (span.SequenceEqual("weight"))
        {
            return TagType.FontWeight;
        }
        else if (span.SequenceEqual("style"))
        {
            return TagType.FontStyle;
        }
        else if (span.SequenceEqual("b") || span.SequenceEqual("bold"))
        {
            return TagType.FontWeightBold;
        }
        else if (span.SequenceEqual("i") || span.SequenceEqual("italic"))
        {
            return TagType.FontStyleItalic;
        }
        else if (span.SequenceEqual("noparse"))
        {
            return TagType.NoParse;
        }
        else if (span.StartsWith("#", StringComparison.Ordinal))
        {
            return TagType.ColorHash;
        }

        return TagType.Invalid;
    }

    public record struct Token(StringSpan Text, TokenType Type)
    {
        public override string ToString()
        {
            if (Type == TokenType.NewLine)
            {
                return "newline";
            }
            return Text.AsSpan().ToString();
        }
    }

    public enum TokenType
    {
        TagStart,
        TagClose,
        Content,
        NewLine,
    }

    public record struct TagInfo(StringSpan Value, TagType Type)
    {
        public static readonly TagInfo Invalid = new(StringSpan.Empty, TagType.Invalid);

        public bool TryGetColor(out Color color)
        {
            if (Type is TagType.Color or TagType.ColorHash &&
                Color.TryParse(RemoveQuotation(Value), out color))
            {
                return true;
            }

            color = default;
            return false;
        }

        public bool TryGetFont(out FontFamily font)
        {
            if (Type == TagType.Font)
            {
                ReadOnlySpan<char> str = RemoveQuotation(Value);
                font = new FontFamily(str.ToString());
                if (FontManager.Instance.FontFamilies.Contains(font))
                {
                    return true;
                }
            }

            font = default;
            return false;
        }

        public bool TryGetSize(out float size)
        {
            if (Type is TagType.Size &&
                float.TryParse(RemoveQuotation(Value), NumberStyles.Float, CultureInfo.InvariantCulture, out size))
            {
                return true;
            }

            size = 0;
            return false;
        }

        public bool TryGetCharSpace(out float space)
        {
            if (Type is TagType.CharSpace &&
                float.TryParse(RemoveQuotation(Value), NumberStyles.Float, CultureInfo.InvariantCulture, out space))
            {
                return true;
            }

            space = 0;
            return false;
        }

        public bool TryGetMargin(out Thickness margin)
        {
            if (Type is TagType.Margin &&
                Thickness.TryParse(RemoveQuotation(Value), out margin))
            {
                return true;
            }

            margin = default;
            return false;
        }

        public bool TryGetFontWeight(out FontWeight weight)
        {
            if (Type is TagType.FontWeight or TagType.FontWeightBold)
            {
                ReadOnlySpan<char> str = RemoveQuotation(Value);
                if (int.TryParse(str, out int weightNum))
                {
                    weight = (FontWeight)weightNum;
                    return true;
                }
                else
                {
                    weight = str switch
                    {
                        var thin when thin.SequenceEqual("thin") => FontWeight.Thin,
                        var ulight when ulight.SequenceEqual("ultra-light") => FontWeight.UltraLight,
                        var light when light.SequenceEqual("light") => FontWeight.Light,
                        var slight when slight.SequenceEqual("semi-light") => FontWeight.SemiLight,
                        var reg when reg.SequenceEqual("regular") => FontWeight.Regular,
                        var med when med.SequenceEqual("medium") => FontWeight.Medium,
                        var sbold when sbold.SequenceEqual("semi-bold") => FontWeight.SemiBold,
                        var bold when bold.SequenceEqual("bold") => FontWeight.Bold,
                        var b when b.SequenceEqual("b") => FontWeight.Bold,
                        var ubold when ubold.SequenceEqual("ultra-bold") => FontWeight.UltraBold,
                        var black when black.SequenceEqual("black") => FontWeight.Black,
                        var ublack when ublack.SequenceEqual("ultra-black") => FontWeight.UltraBlack,
                        _ => (FontWeight)(-1),
                    };

                    if ((int)weight != -1)
                    {
                        return true;
                    }
                }
            }

            weight = FontWeight.Regular;
            return false;
        }

        public bool TryGetFontStyle(out FontStyle style)
        {
            if (Type is TagType.FontStyle or TagType.FontStyleItalic)
            {
                ReadOnlySpan<char> str = RemoveQuotation(Value);
                style = str switch
                {
                    var normal when normal.SequenceEqual("normal") => FontStyle.Normal,
                    var italic when italic.SequenceEqual("italic") => FontStyle.Italic,
                    var i when i.SequenceEqual("i") => FontStyle.Italic,
                    var oblique when oblique.SequenceEqual("oblique") => FontStyle.Oblique,
                    _ => (FontStyle)(-1),
                };

                if ((int)style != -1)
                {
                    return true;
                }
            }

            style = FontStyle.Normal;
            return false;
        }

        private static ReadOnlySpan<char> RemoveQuotation(StringSpan s)
        {
            ReadOnlySpan<char> span = s.AsSpan();
            if (span.StartsWith("<", StringComparison.Ordinal))
            {
                span = span[1..];
            }
            if (span.EndsWith(">", StringComparison.Ordinal))
            {
                span = span[..^1];
            }
            if (span.StartsWith("\"", StringComparison.Ordinal) ||
                span.StartsWith("\'", StringComparison.Ordinal))
            {
                span = span[1..];
            }
            if (span.EndsWith("\"", StringComparison.Ordinal) ||
               span.EndsWith("\'", StringComparison.Ordinal))
            {
                span = span[..^1];
            }

            return span;
        }
    }

    public enum TagType
    {
        Invalid,
        Font,
        Size,
        Color,
        ColorHash,
        CharSpace,
        FontWeightBold,
        FontWeight,
        FontStyle,
        FontStyleItalic,
        Margin,
        NoParse
    }
}
