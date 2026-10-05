using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Markup;

namespace CrybbBot.Helpers;

public static class RichTextStorageHelper
{
    public static string ToXaml(FlowDocument? document)
    {
        if (document == null)
            return string.Empty;

        return XamlWriter.Save(document);
    }

    public static FlowDocument FromXaml(string? xaml)
    {
        if (string.IsNullOrWhiteSpace(xaml))
            return new FlowDocument();

        using var stringReader = new StringReader(xaml);
        using var xmlReader = System.Xml.XmlReader.Create(stringReader);

        return (FlowDocument)XamlReader.Load(xmlReader);
    }

    public static FlowDocument BuildPreview(string text)
    {
        var doc = new FlowDocument();

        var paragraph = new Paragraph();

        int index = 0;

        foreach (Match m in Regex.Matches(text, @"(\*.*?\*|_.*?_)"))
        {
            if (m.Index > index)
            {
                paragraph.Inlines.Add(new Run(text.Substring(index, m.Index - index)));
            }

            string content = m.Value[1..^1];

            if (m.Value.StartsWith("*"))
                paragraph.Inlines.Add(new Bold(new Run(content)));

            else if (m.Value.StartsWith("_"))
                paragraph.Inlines.Add(new Italic(new Run(content)));

            index = m.Index + m.Length;
        }

        if (index < text.Length)
            paragraph.Inlines.Add(new Run(text.Substring(index)));

        doc.Blocks.Add(paragraph);

        return doc;
    }
}