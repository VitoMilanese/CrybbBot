using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace CrybbBot.Behavior;

public static class SlackRichTextBoxBehavior
{
    public static readonly DependencyProperty EnablePlainTextModeProperty =
        DependencyProperty.RegisterAttached(
            "EnablePlainTextMode",
            typeof(bool),
            typeof(SlackRichTextBoxBehavior),
            new PropertyMetadata(false, OnEnablePlainTextModeChanged));

    private static readonly DependencyProperty IsNormalizingProperty =
        DependencyProperty.RegisterAttached(
            "IsNormalizing",
            typeof(bool),
            typeof(SlackRichTextBoxBehavior),
            new PropertyMetadata(false));

    public static bool GetEnablePlainTextMode(DependencyObject obj)
        => (bool)obj.GetValue(EnablePlainTextModeProperty);

    public static void SetEnablePlainTextMode(DependencyObject obj, bool value)
        => obj.SetValue(EnablePlainTextModeProperty, value);

    private static bool GetIsNormalizing(DependencyObject obj)
        => (bool)obj.GetValue(IsNormalizingProperty);

    private static void SetIsNormalizing(DependencyObject obj, bool value)
        => obj.SetValue(IsNormalizingProperty, value);

    private static void OnEnablePlainTextModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RichTextBox rtb)
            return;

        if ((bool)e.NewValue)
        {
            DataObject.AddPastingHandler(rtb, OnPasting);
            rtb.PreviewKeyDown += OnPreviewKeyDown;
            rtb.TextChanged += OnTextChanged;
            rtb.SizeChanged += OnSizeChanged;
            rtb.Loaded += OnLoaded;

            NormalizeDocument(rtb);
        }
        else
        {
            DataObject.RemovePastingHandler(rtb, OnPasting);
            rtb.PreviewKeyDown -= OnPreviewKeyDown;
            rtb.TextChanged -= OnTextChanged;
            rtb.SizeChanged -= OnSizeChanged;
            rtb.Loaded -= OnLoaded;
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is RichTextBox rtb)
            NormalizeDocument(rtb);
    }

    private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is RichTextBox rtb)
            NormalizeDocument(rtb);
    }

    //private static void OnPasting(object sender, DataObjectPastingEventArgs e)
    //{
    //    if (sender is not RichTextBox rtb)
    //        return;

    //    if (e.DataObject.GetDataPresent(DataFormats.UnicodeText))
    //    {
    //        var text = e.DataObject.GetData(DataFormats.UnicodeText) as string ?? string.Empty;

    //        e.CancelCommand();
    //        rtb.Selection.Text = text;

    //        NormalizeDocument(rtb);
    //        return;
    //    }

    //    // Block images, files, RTF, XAML, etc.
    //    e.CancelCommand();
    //}

    private static void OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not RichTextBox rtb)
            return;

        // 1) Prefer WPF-native rich formats first
        if (e.DataObject.GetDataPresent(DataFormats.XamlPackage))
        {
            e.CancelCommand();
            PasteRichContent(rtb, e.DataObject, DataFormats.XamlPackage);
            return;
        }

        if (e.DataObject.GetDataPresent(DataFormats.Xaml))
        {
            e.CancelCommand();
            PasteRichContent(rtb, e.DataObject, DataFormats.Xaml);
            return;
        }

        // 2) Optional: allow RTF too
        if (e.DataObject.GetDataPresent(DataFormats.Rtf))
        {
            e.CancelCommand();
            PasteRichContent(rtb, e.DataObject, DataFormats.Rtf);
            return;
        }

        // 3) Fallback to plain text
        if (e.DataObject.GetDataPresent(DataFormats.UnicodeText))
        {
            var text = e.DataObject.GetData(DataFormats.UnicodeText) as string ?? string.Empty;

            e.CancelCommand();
            rtb.Selection.Text = text;

            SlackRichTextBoxBehavior.NormalizeDocument(rtb);
            return;
        }

        // 4) Block everything else
        e.CancelCommand();
    }

    private static void PasteRichContent(RichTextBox rtb, IDataObject dataObject, string format)
    {
        try
        {
            var tempDoc = new FlowDocument();
            var tempRange = new TextRange(tempDoc.ContentStart, tempDoc.ContentEnd);

            if (format == DataFormats.XamlPackage)
            {
                if (dataObject.GetData(format) is MemoryStream ms)
                {
                    tempRange.Load(ms, DataFormats.XamlPackage);
                }
                else if (dataObject.GetData(format) is byte[] bytes)
                {
                    using var stream = new MemoryStream(bytes);
                    tempRange.Load(stream, DataFormats.XamlPackage);
                }
                else
                {
                    return;
                }
            }
            else if (format == DataFormats.Xaml)
            {
                if (dataObject.GetData(format) is string xaml)
                {
                    using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xaml));
                    tempRange.Load(stream, DataFormats.Xaml);
                }
                else
                {
                    return;
                }
            }
            else if (format == DataFormats.Rtf)
            {
                if (dataObject.GetData(format) is string rtf)
                {
                    using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(rtf));
                    tempRange.Load(stream, DataFormats.Rtf);
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }

            SanitizeDocument(tempDoc);
            SlackRichTextBoxBehavior.NormalizeTempDocument(tempDoc, rtb);

            var sanitizedRange = new TextRange(tempDoc.ContentStart, tempDoc.ContentEnd);

            using var outStream = new MemoryStream();
            sanitizedRange.Save(outStream, DataFormats.Xaml);
            outStream.Position = 0;

            rtb.Selection.Load(outStream, DataFormats.Xaml);

            SlackRichTextBoxBehavior.NormalizeDocument(rtb);
        }
        catch
        {
            // optional fallback
        }
    }

    private static void SanitizeDocument(FlowDocument doc)
    {
        foreach (var block in doc.Blocks.ToList())
        {
            SanitizeBlock(block);
        }
    }

    private static void SanitizeBlock(Block block)
    {
        switch (block)
        {
            case Paragraph paragraph:
                foreach (var inline in paragraph.Inlines.ToList())
                {
                    SanitizeInline(inline, paragraph);
                }
                break;

            case Section section:
                foreach (var child in section.Blocks.ToList())
                {
                    SanitizeBlock(child);
                }
                break;

            case List list:
                foreach (var item in list.ListItems)
                {
                    foreach (var child in item.Blocks.ToList())
                    {
                        SanitizeBlock(child);
                    }
                }
                break;

            case BlockUIContainer blockUi:
                if (block.Parent is Section parentSection)
                    parentSection.Blocks.Remove(blockUi);
                break;
        }
    }

    private static void SanitizeInline(Inline inline, Paragraph parentParagraph)
    {
        switch (inline)
        {
            case InlineUIContainer ui:
                parentParagraph.Inlines.Remove(ui);
                break;

            case Span span:
                foreach (var child in span.Inlines.ToList())
                {
                    SanitizeInline(child, parentParagraph);
                }
                break;
        }
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not RichTextBox rtb)
            return;

        if (e.Key == Key.Return && Keyboard.Modifiers == ModifierKeys.Shift)
        {
            e.Handled = true;

            var pos = rtb.CaretPosition;
            pos.InsertLineBreak();

            var next = pos.GetNextInsertionPosition(LogicalDirection.Forward);
            if (next != null)
                rtb.CaretPosition = next;

            NormalizeDocument(rtb);
            return;
        }

        if (e.Key == Key.Return && Keyboard.Modifiers == ModifierKeys.None)
        {
            e.Handled = true;

            rtb.BeginChange();
            try
            {
                var currentParagraph = rtb.CaretPosition.Paragraph;
                bool exitQuote = currentParagraph != null &&
                                 currentParagraph.TextIndent > 0 &&
                                 IsParagraphEmpty(currentParagraph);

                var pos = rtb.CaretPosition;
                pos.InsertParagraphBreak();

                var next = pos.GetNextInsertionPosition(LogicalDirection.Forward);
                if (next != null)
                    rtb.CaretPosition = next;

                var newParagraph = rtb.CaretPosition.Paragraph;
                if (newParagraph != null)
                {
                    if (exitQuote)
                        RemoveQuoteStyle(newParagraph);
                    else if (currentParagraph != null && currentParagraph.TextIndent > 0)
                        ApplyQuoteStyle(newParagraph);
                }
            }
            finally
            {
                rtb.EndChange();
            }

            NormalizeDocument(rtb);
            return;
        }

        if (e.Key == Key.Back && Keyboard.Modifiers == ModifierKeys.None)
        {
            var paragraph = rtb.CaretPosition.Paragraph;

            if (paragraph != null &&
                paragraph.TextIndent > 0 &&
                IsCaretAtParagraphStart(rtb.CaretPosition, paragraph) &&
                IsParagraphEmpty(paragraph))
            {
                e.Handled = true;

                rtb.BeginChange();
                try
                {
                    RemoveQuoteStyle(paragraph);
                }
                finally
                {
                    rtb.EndChange();
                }

                NormalizeDocument(rtb);
                return;
            }
        }
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not RichTextBox rtb)
            return;

        if (GetIsNormalizing(rtb))
            return;

        NormalizeDocument(rtb);
    }

    public static void NormalizeDocument(RichTextBox rtb)
    {
        if (GetIsNormalizing(rtb))
            return;

        try
        {
            SetIsNormalizing(rtb, true);

            var doc = rtb.Document;
            if (doc == null)
                return;

            doc.PagePadding = new Thickness(0);
            doc.ColumnGap = 0;
            doc.MinPageWidth = 0;
            doc.MaxPageWidth = double.PositiveInfinity;

            var width = rtb.ViewportWidth;
            if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
                width = rtb.ActualWidth;

            if (!double.IsNaN(width) && !double.IsInfinity(width) && width > 0)
                doc.PageWidth = Math.Max(0, width - rtb.Padding.Left - rtb.Padding.Right - 2);

            doc.FontFamily = rtb.FontFamily;
            doc.FontSize = rtb.FontSize;
            doc.LineHeight = double.NaN;
            doc.TextAlignment = TextAlignment.Left;

            foreach (var block in doc.Blocks.ToList())
            {
                NormalizeBlock(block, rtb);
            }
        }
        finally
        {
            SetIsNormalizing(rtb, false);
        }
    }

    private static void NormalizeBlock(Block block, RichTextBox rtb)
    {
        switch (block)
        {
            case Paragraph paragraph:
                NormalizeParagraph(paragraph, rtb);
                break;

            case Section section:
                section.Margin = new Thickness(0);
                foreach (var child in section.Blocks.ToList())
                {
                    NormalizeBlock(child, rtb);
                }
                break;

            case List list:
                list.Margin = new Thickness(0);
                foreach (var item in list.ListItems)
                {
                    foreach (var child in item.Blocks.ToList())
                    {
                        NormalizeBlock(child, rtb);
                    }
                }
                break;
        }
    }

    private static void NormalizeParagraph(Paragraph paragraph, RichTextBox rtb)
    {
        paragraph.Margin = new Thickness(0);
        paragraph.TextAlignment = TextAlignment.Left;
        paragraph.LineHeight = double.NaN;
        paragraph.LineStackingStrategy = LineStackingStrategy.MaxHeight;
        paragraph.FontFamily = rtb.FontFamily;
        paragraph.FontSize = rtb.FontSize;

        foreach (var inline in paragraph.Inlines.ToList())
        {
            NormalizeInline(inline, rtb, paragraph);
        }
    }

    private static void NormalizeInline(Inline inline, RichTextBox rtb, Paragraph parentParagraph)
    {
        switch (inline)
        {
            case Run run:
                run.FontFamily = rtb.FontFamily;
                run.FontSize = rtb.FontSize;
                run.Foreground = rtb.Foreground;
                run.ClearValue(TextElement.BackgroundProperty);
                break;

            case Hyperlink link:
                link.FontFamily = rtb.FontFamily;
                link.FontSize = rtb.FontSize;

                // keep editor color if you want, but preserve visual link style
                //link.Foreground = rtb.Foreground;
                link.Foreground = Brushes.DeepSkyBlue;
                link.TextDecorations = TextDecorations.Underline;

                foreach (var child in link.Inlines.ToList())
                {
                    NormalizeInline(child, rtb, parentParagraph);
                }
                break;

            case Span span:
                span.FontFamily = rtb.FontFamily;
                span.FontSize = rtb.FontSize;
                span.Foreground = rtb.Foreground;
                span.ClearValue(TextElement.BackgroundProperty);

                foreach (var child in span.Inlines.ToList())
                {
                    NormalizeInline(child, rtb, parentParagraph);
                }
                break;

            case InlineUIContainer ui:
                parentParagraph.Inlines.Remove(ui);
                break;
        }
    }

    private static void NormalizeTempDocument(FlowDocument doc, RichTextBox rtb)
    {
        doc.PagePadding = new Thickness(0);
        doc.FontFamily = rtb.FontFamily;
        doc.FontSize = rtb.FontSize;
        doc.TextAlignment = TextAlignment.Left;

        foreach (var paragraph in doc.Blocks.OfType<Paragraph>())
        {
            paragraph.Margin = new Thickness(0);
            paragraph.LineHeight = double.NaN;
            paragraph.LineStackingStrategy = LineStackingStrategy.MaxHeight;
        }
    }

    private static bool IsParagraphEmpty(Paragraph paragraph)
    {
        var text = new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text;
        return string.IsNullOrWhiteSpace(text);
    }

    private static bool IsCaretAtParagraphStart(TextPointer caret, Paragraph paragraph)
    {
        var start = paragraph.ContentStart.GetInsertionPosition(LogicalDirection.Forward);
        var current = caret.GetInsertionPosition(LogicalDirection.Backward);

        return start != null && current != null && start.CompareTo(current) == 0;
    }

    public static bool IsQuotedParagraph(Paragraph p)
    {
        return p.TextIndent > 0;
    }

    public static void ApplyQuoteStyle(Paragraph p)
    {
        p.TextIndent = 20;
        p.Margin = new Thickness(0, 2, 0, 2);
        p.Padding = new Thickness(10, 2, 0, 2);
        p.BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120));
        p.BorderThickness = new Thickness(3, 0, 0, 0);
        p.Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));
    }

    public static void RemoveQuoteStyle(Paragraph p)
    {
        p.TextIndent = 0;
        p.Margin = new Thickness(0);
        p.Padding = new Thickness(0);
        p.ClearValue(Block.BorderBrushProperty);
        p.ClearValue(Block.BorderThicknessProperty);
        p.ClearValue(Block.BackgroundProperty);
    }
}