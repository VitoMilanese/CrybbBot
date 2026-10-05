using System.Text.Json.Serialization;

namespace SlackBotSender.Slack;

public sealed class SlackRichTextBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "rich_text";

    [JsonPropertyName("elements")]
    public List<SlackRichBlockElementBase> Elements { get; set; } = new();
}

public abstract class SlackRichBlockElementBase
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public sealed class SlackRichTextSection : SlackRichBlockElementBase
{
    public SlackRichTextSection()
    {
        Type = "rich_text_section";
    }

    [JsonPropertyName("elements")]
    public List<SlackRichTextElementBase> Elements { get; set; } = new();
}

public sealed class SlackRichTextList : SlackRichBlockElementBase
{
    public SlackRichTextList()
    {
        Type = "rich_text_list";
    }

    [JsonPropertyName("style")]
    public string Style { get; set; } = "bullet";

    [JsonPropertyName("elements")]
    public List<SlackRichTextSection> Elements { get; set; } = new();
}

public sealed class SlackRichTextQuote : SlackRichBlockElementBase
{
    public SlackRichTextQuote()
    {
        Type = "rich_text_quote";
    }

    [JsonPropertyName("elements")]
    public List<SlackRichTextElementBase> Elements { get; set; } = new();
}

public abstract class SlackRichTextElementBase
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public sealed class SlackTextElement : SlackRichTextElementBase
{
    public SlackTextElement()
    {
        Type = "text";
    }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("style")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SlackTextStyle? Style { get; set; }
}

public sealed class SlackLinkElement : SlackRichTextElementBase
{
    public SlackLinkElement()
    {
        Type = "link";
    }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    [JsonPropertyName("style")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SlackTextStyle? Style { get; set; }
}

public sealed class SlackTextStyle
{
    [JsonPropertyName("bold")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Bold { get; set; }

    [JsonPropertyName("italic")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Italic { get; set; }

    [JsonPropertyName("strike")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Strike { get; set; }

    [JsonPropertyName("underline")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Underline { get; set; }

    [JsonPropertyName("code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Code { get; set; }

    [JsonIgnore]
    public bool HasAnyValue => Bold || Italic || Strike || Underline || Code;
}
