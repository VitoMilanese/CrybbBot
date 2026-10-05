using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;

namespace CrybbBot.Helpers;

public static class SlackRichTextBlockConverter
{
    public static object ToSlackRichTextBlockObject(FlowDocument? document)
    {
        var block = new Dictionary<string, object?>
        {
            ["type"] = "rich_text",
            ["elements"] = new List<object>()
        };

        var elements = (List<object>)block["elements"]!;

        if (document == null)
            return block;

        foreach (var wpfBlock in document.Blocks)
        {
            switch (wpfBlock)
            {
                case Paragraph paragraph:
                    elements.Add(ConvertParagraph(paragraph));
                    break;

                case List list:
                    elements.Add(ConvertList(list));
                    break;

                case Section section:
                    foreach (var child in section.Blocks)
                    {
                        switch (child)
                        {
                            case Paragraph p:
                                elements.Add(ConvertParagraph(p));
                                break;

                            case List l:
                                elements.Add(ConvertList(l));
                                break;

                            default:
                                elements.Add(ConvertFallbackBlock(child));
                                break;
                        }
                    }
                    break;

                default:
                    elements.Add(ConvertFallbackBlock(wpfBlock));
                    break;
            }
        }

        return block;
    }

    private static object ConvertParagraph(Paragraph paragraph)
    {
        if (paragraph.TextIndent > 0)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "rich_text_quote",
                ["elements"] = ConvertInlines(paragraph.Inlines)
            };
        }

        return new Dictionary<string, object?>
        {
            ["type"] = "rich_text_section",
            ["elements"] = ConvertInlines(paragraph.Inlines)
        };
    }

    private static object ConvertList(List list)
    {
        var items = new List<object>();

        foreach (var item in list.ListItems)
        {
            var itemElements = new List<object>();

            foreach (var paragraph in item.Blocks.OfType<Paragraph>())
            {
                itemElements.AddRange(ConvertInlines(paragraph.Inlines));
            }

            items.Add(new Dictionary<string, object?>
            {
                ["type"] = "rich_text_section",
                ["elements"] = itemElements
            });
        }

        return new Dictionary<string, object?>
        {
            ["type"] = "rich_text_list",
            ["style"] = list.MarkerStyle == TextMarkerStyle.Decimal ? "ordered" : "bullet",
            ["elements"] = items
        };
    }

    private static object ConvertFallbackBlock(Block block)
    {
        var text = new TextRange(block.ContentStart, block.ContentEnd).Text.TrimEnd('\r', '\n');

        return new Dictionary<string, object?>
        {
            ["type"] = "rich_text_section",
            ["elements"] = new List<object>
            {
                CreateTextElement(text, null)
            }
        };
    }

    private static List<object> ConvertInlines(InlineCollection inlines)
    {
        var result = new List<object>();

        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case Run run:
                    if (!string.IsNullOrEmpty(run.Text))
                    {
                        result.Add(CreateTextElement(run.Text, BuildStyle(run)));
                    }
                    break;

                case LineBreak:
                    result.Add(CreateTextElement("\n", null));
                    break;

                case Hyperlink hyperlink:
                    result.Add(ConvertHyperlink(hyperlink));
                    break;

                case Bold bold:
                    result.AddRange(ConvertSpanLike(bold, style => style["bold"] = true));
                    break;

                case Italic italic:
                    result.AddRange(ConvertSpanLike(italic, style => style["italic"] = true));
                    break;

                case Underline underline:
                    result.AddRange(ConvertSpanLike(underline, style => style["underline"] = true));
                    break;

                case Span span:
                    result.AddRange(ConvertSpanLike(span, style =>
                    {
                        if (HasStrikethrough(span))
                            style["strike"] = true;
                    }));
                    break;

                default:
                    var text = new TextRange(inline.ContentStart, inline.ContentEnd).Text;
                    if (!string.IsNullOrEmpty(text))
                    {
                        result.Add(CreateTextElement(text, null));
                    }
                    break;
            }
        }

        return MergeAdjacentTextElements(result);
    }

    private static IEnumerable<object> ConvertSpanLike(Span span, Action<Dictionary<string, object>> mutateStyle)
    {
        var converted = ConvertInlines(span.Inlines);

        foreach (var element in converted.OfType<Dictionary<string, object?>>())
        {
            if (!element.TryGetValue("type", out var typeObj) || typeObj is not string type)
                continue;

            if (type is "text" or "link")
            {
                if (!element.TryGetValue("style", out var styleObj) || styleObj is not Dictionary<string, object> style)
                {
                    style = new Dictionary<string, object>();
                    element["style"] = style;
                }

                mutateStyle(style);

                if (style.Count == 0)
                    element["style"] = null;
            }
        }

        return converted;
    }

    private static object ConvertHyperlink(Hyperlink hyperlink)
    {
        var text = new TextRange(hyperlink.ContentStart, hyperlink.ContentEnd).Text.TrimEnd('\r', '\n');

        var element = new Dictionary<string, object?>
        {
            ["type"] = "link",
            ["url"] = hyperlink.NavigateUri?.AbsoluteUri ?? text
        };

        if (!string.IsNullOrWhiteSpace(text))
            element["text"] = text;

        var style = BuildStyle(hyperlink);
        if (style != null && style.Count > 0)
            element["style"] = style;

        return element;
    }

    private static Dictionary<string, object?> CreateTextElement(string text, Dictionary<string, object>? style)
    {
        var element = new Dictionary<string, object?>
        {
            ["type"] = "text",
            ["text"] = text
        };

        if (style != null && style.Count > 0)
            element["style"] = style;

        return element;
    }

    private static Dictionary<string, object>? BuildStyle(Inline inline)
    {
        var style = new Dictionary<string, object>();

        var weight = inline.FontWeight;
        if (weight == FontWeights.Bold || weight == FontWeights.ExtraBold || weight == FontWeights.Black)
            style["bold"] = true;

        if (inline.FontStyle == FontStyles.Italic || inline.FontStyle == FontStyles.Oblique)
            style["italic"] = true;

        foreach (var d in inline.TextDecorations)
        {
            if (d.Location == TextDecorationLocation.Underline)
                style["underline"] = true;

            if (d.Location == TextDecorationLocation.Strikethrough)
                style["strike"] = true;
        }

        return style.Count > 0 ? style : null;
    }

    private static bool HasStrikethrough(Inline inline)
    {
        foreach (var d in inline.TextDecorations)
        {
            if (d.Location == TextDecorationLocation.Strikethrough)
                return true;
        }

        return false;
    }

    private static List<object> MergeAdjacentTextElements(List<object> items)
    {
        var result = new List<object>();

        foreach (var item in items)
        {
            if (result.LastOrDefault() is Dictionary<string, object?> prev &&
                item is Dictionary<string, object?> cur &&
                IsTextElement(prev) &&
                IsTextElement(cur) &&
                SameStyle(prev, cur))
            {
                prev["text"] = $"{prev["text"]}{cur["text"]}";
            }
            else
            {
                result.Add(item);
            }
        }

        return result;
    }

    private static bool IsTextElement(Dictionary<string, object?> element)
    {
        return element.TryGetValue("type", out var type) &&
               type is string s &&
               s == "text";
    }

    private static bool SameStyle(Dictionary<string, object?> a, Dictionary<string, object?> b)
    {
        var sa = a.TryGetValue("style", out var styleA) ? styleA as Dictionary<string, object> : null;
        var sb = b.TryGetValue("style", out var styleB) ? styleB as Dictionary<string, object> : null;

        if (sa == null && sb == null) return true;
        if (sa == null || sb == null) return false;
        if (sa.Count != sb.Count) return false;

        foreach (var kv in sa)
        {
            if (!sb.TryGetValue(kv.Key, out var val)) return false;
            if (!Equals(kv.Value, val)) return false;
        }

        return true;
    }
}
