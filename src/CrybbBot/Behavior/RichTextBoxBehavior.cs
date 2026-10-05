using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace CrybbBot.Behavior;

public static class RichTextBoxBehavior
{
    public static readonly DependencyProperty BoundDocumentProperty =
        DependencyProperty.RegisterAttached(
            "BoundDocument",
            typeof(FlowDocument),
            typeof(RichTextBoxBehavior),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnBoundDocumentChanged));

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(RichTextBoxBehavior),
            new PropertyMetadata(false));

    public static void SetBoundDocument(DependencyObject element, FlowDocument? value)
        => element.SetValue(BoundDocumentProperty, value);

    public static FlowDocument? GetBoundDocument(DependencyObject element)
        => (FlowDocument?)element.GetValue(BoundDocumentProperty);

    private static void SetIsUpdating(DependencyObject element, bool value)
        => element.SetValue(IsUpdatingProperty, value);

    private static bool GetIsUpdating(DependencyObject element)
        => (bool)element.GetValue(IsUpdatingProperty);

    private static void OnBoundDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RichTextBox rtb)
            return;

        if (GetIsUpdating(rtb))
            return;

        var doc = e.NewValue as FlowDocument ?? new FlowDocument(new Paragraph());

        // Important: do not reassign the same document instance
        if (ReferenceEquals(rtb.Document, doc))
        {
            SlackRichTextBoxBehavior.NormalizeDocument(rtb);
            return;
        }

        rtb.TextChanged -= RichTextBox_TextChanged;

        rtb.Document = doc;
        SlackRichTextBoxBehavior.NormalizeDocument(rtb);

        rtb.TextChanged += RichTextBox_TextChanged;
    }

    private static void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not RichTextBox rtb)
            return;

        // If VM already points to the same FlowDocument instance,
        // there is nothing to push back.
        if (ReferenceEquals(GetBoundDocument(rtb), rtb.Document))
            return;

        SetIsUpdating(rtb, true);
        SetBoundDocument(rtb, rtb.Document);
        SetIsUpdating(rtb, false);
    }
}