using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using CrybbBot.Behavior;
using CrybbBot.Models;
using CrybbBot.ViewModels;
using Microsoft.Win32;

namespace CrybbBot.Views
{
    /// <summary>
    /// Interaction logic for NewMessageView.xaml
    /// </summary>
    public partial class NewMessageView : UserControl
    {
        private NewMessageViewModel _vm => (NewMessageViewModel)Resources["ViewModel"];

        private static string _clipboardRepo { get; }

        public bool IsEditing => _vm.MessageForEdit != null;

        private ToolTip _linkToolTip { get; } = new();
        private Hyperlink? _hoveredHyperlink;

        static NewMessageView()
        {
            var oAssembly = Assembly.GetExecutingAssembly();
            var root = Path.GetDirectoryName(oAssembly.Location)!;
            _clipboardRepo = Path.Combine(root, "Clipboard");
        }

        public NewMessageView()
        {
            InitializeComponent();

            _linkToolTip.PlacementTarget = tbMessage;
            _linkToolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
            _linkToolTip.StaysOpen = true;

            _vm.RichTextBoxToBeUpdated = tbMessage;

            // Prevent Ctrl+V handler from conflicting with drag-drop
            // PreviewKeyDown is on the UserControl, while focus is often inside the TextBox
            // PreviewKeyDown should still bubble/tunnel, but sometimes you may want to attach at root:
            // 'true' ensures you get it even if a child marks it handled
            AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(Window_PreviewKeyDown), true);

            Loaded += NewMessageView_Loaded;
            IsVisibleChanged += NewMessageView_IsVisibleChanged;
        }

        public void LoadChannels() => _vm.LoadChannels();

        public async Task<bool> StartEditing(Guid messageId)
        {
            var result = await _vm.StartEditing(messageId);
            _comboBox.IsEnabled = !_vm.AlreadySent;
            return result;
        }

        public new void Focus() => tbMessage.Focus();

        public void Reset()
        {
            _vm.Clear();
        }

        private void NewMessageView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= NewMessageView_Loaded;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                tbMessage.Document = _vm.MsgBodyRich;
                SlackRichTextBoxBehavior.NormalizeDocument(tbMessage);
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void NewMessageView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _comboBox.IsEnabled = true;
        }

        private async void AddAttachment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || !byte.TryParse(btn.Tag?.ToString(), out var tag))
            {
                return;
            }

            switch (tag)
            {
                case 0:
                    {
                        var ofd = new OpenFileDialog
                        {
                            Multiselect = false,
                            Title = "Оберіть файл для прикріплення"
                        };
                        if (ofd.ShowDialog() ?? false)
                        {
                            var child = new AttachmentView(ofd.FileName)
                            {
                                Margin = new Thickness(5)
                            };
                            child.DeleteClicked = DeleteAttachment_Click;
                            _vm.Attachments.Add(child);
                        }
                        break;
                    }
                case 1:
                    {
                        var res = await MainWindow.ShowFileReferenceDialog(32);

                        if (res.Yes)
                        {
                            _vm.AddReference(new FileReference
                            {
                                Title = res.Title,
                                Url = res.Url
                            });
                        }

                        break;
                    }
            }
        }

        private void DeleteAttachment_Click(object? sender, EventArgs e)
        {
            if (sender is not AttachmentView attachment) return;
            _vm.Attachments.Remove(attachment);
            if (!string.IsNullOrWhiteSpace(attachment.FilePath) &&
                File.Exists(attachment.FilePath) &&
                attachment.FilePath.StartsWith(_clipboardRepo, StringComparison.InvariantCultureIgnoreCase))
            {
                File.Delete(attachment.FilePath);

                var dir = Path.GetDirectoryName(attachment.FilePath);
                if (!string.IsNullOrWhiteSpace(dir) &&
                    !dir.Equals(_clipboardRepo, StringComparison.InvariantCultureIgnoreCase) &&
                    dir.StartsWith(_clipboardRepo, StringComparison.InvariantCultureIgnoreCase))
                {
                    CleanupDirectory(dir);
                }
            }
        }

        private void CleanupDirectory(string path)
        {
            if (path.Equals(_clipboardRepo, StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            if (!path.StartsWith(_clipboardRepo, StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            if (!files.Any())
            {
                Directory.Delete(path);
            }

            var up = Path.GetDirectoryName(path);
            
            if (!string.IsNullOrWhiteSpace(up))
            {
                CleanupDirectory(up);
            }
        }

        private async void Clear_Click(object sender, RoutedEventArgs e)
        {
            await _vm.ClearAsync();
        }

        private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+V (also handle Key.System in case of Alt combos etc.)
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
                (e.Key == Key.V || e.Key == Key.System && e.SystemKey == Key.V))
            {
                // If you want to prevent the paste going into a focused textbox, uncomment:
                // e.Handled = true;

                var clipboardElements = await Helpers.ClipboardSave.TrySaveClipboardContentAsync(_clipboardRepo);
                if (clipboardElements?.Any() ?? false)
                {
                    foreach (var element in clipboardElements)
                    {
                        var child = new AttachmentView(element.Path, element.OriginalPath, element.ContentType == Helpers.ClipboardSave.ContentType.File)
                        {
                            Margin = new Thickness(5)
                        };
                        child.DeleteClicked = DeleteAttachment_Click;
                        _vm.Attachments.Add(child);
                    }
                }
            }
        }

        private void Attachments_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true; // important, or cursor can show "not allowed"
        }

        private async void Attachments_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            var dropped = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (!dropped.Any())
            {
                return;
            }

            Directory.CreateDirectory(_clipboardRepo);

            foreach (var drop in dropped)
            {
                if (Directory.Exists(drop))
                {
                    await ManageDroppedFolder(drop, drop);
                }
                else if (File.Exists(drop))
                {
                    // copy into your repo so you control lifecycle,
                    // and so original can be moved/deleted without breaking your attachment.
                    var saved = await CopyToClipboardRepoAsync(drop);
                    if (!string.IsNullOrWhiteSpace(saved))
                    {
                        AddAttachmentView(saved, drop);
                    }
                }
            }
        }

        private async Task ManageDroppedFolder(string path, string? root = null)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            var dirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
            foreach (var dir in dirs)
            {
                await ManageDroppedFolder(dir, root);
            }

            var files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                // copy into your repo so you control lifecycle,
                // and so original can be moved/deleted without breaking your attachment.
                var saved = await CopyToClipboardRepoAsync(file, root);
                if (!string.IsNullOrWhiteSpace(saved))
                {
                    AddAttachmentView(saved, file);
                }
            }
        }

        private void AddAttachmentView(string path, string? originalPath = null)
        {
            if (_vm.Attachments
                .Any(a => !string.IsNullOrWhiteSpace(a.FilePath) &&
                     a.FilePath.Equals(path, StringComparison.InvariantCultureIgnoreCase)))
            {
                return;
            }

            foreach (var attachment in _vm.Attachments)
            {
                if (!string.IsNullOrWhiteSpace(attachment.FilePath) &&
                    attachment.FilePath.Equals(path, StringComparison.InvariantCultureIgnoreCase))
                {
                    return;
                }
            }

            var child = new AttachmentView(path, originalPath)
            {
                Margin = new Thickness(5)
            };
            child.DeleteClicked = DeleteAttachment_Click;
            _vm.Attachments.Add(child);
        }

        private static string MakeSafeFileName(string fileName)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(c, '_');
            return fileName;
        }

        private async Task<string> CopyToClipboardRepoAsync(string srcPath, string? root = null)
        {
            var repo = _clipboardRepo;

            if (!string.IsNullOrWhiteSpace(root) && srcPath.StartsWith(root, StringComparison.InvariantCultureIgnoreCase))
            {
                var dirA = Path.GetDirectoryName(srcPath);
                var a = new List<string>();
                while (!string.IsNullOrWhiteSpace(dirA))
                {
                    var path = Path.GetFileName(dirA);
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        a.Insert(0, path);
                        dirA = Path.GetDirectoryName(dirA);
                    }
                    else
                    {
                        break;
                    }
                }

                var b = new List<string>();
                while (!string.IsNullOrWhiteSpace(root))
                {
                    var path = Path.GetFileName(root);
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        b.Insert(0, path);
                        root = Path.GetDirectoryName(root);
                    }
                    else
                    {
                        break;
                    }
                }

                for (var i = 0; i < b.Count - 1; ++i)
                {
                    a.RemoveAt(0);
                }

                if (a.Count > 0)
                {
                    for (var i = 0; i < a.Count; ++i)
                    {
                        repo = Path.Combine(repo, a[i]);
                    }
                    Directory.CreateDirectory(repo);
                }
            }

            var dstName = MakeSafeFileName(Path.GetFileName(srcPath));
            var dstPath = Path.Combine(repo, dstName);

            var unique = GetUniquePath(dstPath);

            if (!Path.GetFileName(unique).Equals(Path.GetFileName(srcPath), StringComparison.InvariantCultureIgnoreCase))
            {
                return dstPath;
            }

            dstPath = unique;

            try
            {
                // Copy in a way that doesn't prevent the source from being deleted
                using var src = new FileStream(srcPath, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                using var dst = new FileStream(dstPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);

                await src.CopyToAsync(dst);
                return dstPath;
            }
            catch (UnauthorizedAccessException)
            {
                await MainWindow.ShowDialog("Помилка", $"Не вдалося отримати доступ до файлу {Path.GetFileName(srcPath)}.\r\nЦей файл зайнятий іншим процесом.");
                return string.Empty;
            }
            catch (Exception)
            {
                await MainWindow.ShowDialog("Помилка", $"Не вдалося прочитати файл {Path.GetFileName(srcPath)}.");
                return string.Empty;
            }
        }

        private static string GetUniquePath(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                return path;

            var dir = Path.GetDirectoryName(path)!;
            var name = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);

            for (int i = 1; i < 10_000; i++)
            {
                var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
                if (!File.Exists(candidate) && !Directory.Exists(candidate))
                    return candidate;
            }

            return Path.Combine(dir, $"{name}_{Guid.NewGuid():N}{ext}");
        }

        private async void EditReference_CLick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn ||
                btn.DataContext is not FileReference reference)
            {
                return;
            }

            var res = await MainWindow.ShowEditFileReferenceDialog(32, reference.Title ?? string.Empty, reference.Url ?? string.Empty);

            if (res.Yes)
            {
                reference.Title = res.Title;
                reference.Url = res.Url;
            }
        }

        private void RemoveReference_CLick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn ||
                btn.DataContext is not FileReference reference)
            {
                return;
            }

            _vm.RemoveReference(reference);
        }

        private void TbMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vm.RaisePropertyChanged("CanSend");
        }

        /// <summary>
        /// Selection → Result
        /// Plain text → strike applied
        /// Already struck → strike removed
        /// Mixed selection → toggled
        /// Caret only → nothing happens
        /// </summary>
        private void Strike_Click(object sender, RoutedEventArgs e)
        {
            var selection = tbMessage.Selection;

            if (selection == null || selection.IsEmpty)
                return;

            var current = selection.GetPropertyValue(Inline.TextDecorationsProperty);

            bool hasStrike = false;

            if (current != DependencyProperty.UnsetValue &&
                current is TextDecorationCollection decorations)
            {
                hasStrike = decorations.Any(d => d.Location == TextDecorationLocation.Strikethrough);
            }

            if (hasStrike)
            {
                selection.ApplyPropertyValue(Inline.TextDecorationsProperty, null);
            }
            else
            {
                selection.ApplyPropertyValue(
                    Inline.TextDecorationsProperty,
                    TextDecorations.Strikethrough);
            }

            _vm.FocusTextBox = true;
        }

        /// <summary>
        /// No selected link → ask for URL and create link
        /// Selected text inside existing link → open dialog with current URL
        /// Dialog result:
        ///     empty / cancel → do nothing
        ///     new URL → update link
        ///     remove flag → unwrap hyperlink, keep text
        /// </summary>
        private async void InsertLink_Click(object sender, RoutedEventArgs e)
        {
            var selection = tbMessage.Selection;

            // Save selection BEFORE dialog steals focus
            var selStart = selection.Start;
            var selEnd = selection.End;
            bool hadSelection = !selection.IsEmpty;
            string selectedText = selection.Text?.Trim() ?? string.Empty;

            var existingLink = GetSelectedHyperlink(tbMessage);

            string initialUrl =
                existingLink?.NavigateUri?.AbsoluteUri
                ?? (Uri.TryCreate(selectedText, UriKind.Absolute, out _) ? selectedText : string.Empty);

            var dlgResult = await MainWindow.ShowLinkDialog(initialUrl, _vm.MessageForEdit != null);

            if (!dlgResult.Yes)
            {
                _vm.FocusTextBox = true;
                return;
            }

            var url = dlgResult.Url?.Trim();

            tbMessage.BeginChange();
            try
            {
                // Empty or invalid URL => remove existing link if any
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    if (existingLink != null)
                        RemoveHyperlink(existingLink);

                    return;
                }

                // Edit existing link
                if (existingLink != null)
                {
                    existingLink.NavigateUri = uri;
                    existingLink.ToolTip = uri.AbsoluteUri;
                    existingLink.TextDecorations = TextDecorations.Underline;
                    return;
                }

                // Create link over the ORIGINAL saved selection
                if (hadSelection && selStart != null && selEnd != null && selStart.CompareTo(selEnd) != 0)
                {
                    var hyperlink = new Hyperlink(selStart, selEnd)
                    {
                        NavigateUri = uri,
                        TextDecorations = TextDecorations.Underline
                    };

                    return;
                }

                // No original selection -> insert URL as link text
                var start = tbMessage.CaretPosition;
                start.InsertTextInRun(url);

                var end = start.GetPositionAtOffset(url.Length, LogicalDirection.Forward);
                if (end != null)
                {
                    var hyperlink = new Hyperlink(start, end)
                    {
                        NavigateUri = uri,
                        TextDecorations = TextDecorations.Underline,
                        ToolTip = uri.AbsoluteUri
                    };

                    tbMessage.CaretPosition = hyperlink.ElementEnd;
                }
            }
            finally
            {
                tbMessage.EndChange();
            }

            _vm.FocusTextBox = true;
        }

        /// <summary>
        /// If all selected paragraphs are already quoted, click removes quote
        /// otherwise, click applies quote to all selected paragraphs
        /// if there is no selection, it affects the current paragraph.
        /// Clicking Quote should remove quote only from the current paragraph.
        /// Pressing Backspace at the start of that paragraph should also remove quote styling completely.
        /// </summary>
        private void Quote_Click(object sender, RoutedEventArgs e)
        {
            var selection = tbMessage.Selection;

            List<Paragraph> paragraphs;

            if (selection.IsEmpty)
            {
                var current = tbMessage.CaretPosition.Paragraph;
                if (current == null)
                    return;

                paragraphs = new List<Paragraph> { current };
            }
            else
            {
                paragraphs = GetSelectedParagraphs(tbMessage).ToList();
                if (paragraphs.Count == 0)
                    return;
            }

            bool allQuoted = paragraphs.All(SlackRichTextBoxBehavior.IsQuotedParagraph);

            tbMessage.BeginChange();
            try
            {
                foreach (var p in paragraphs)
                {
                    if (allQuoted)
                    {
                        SlackRichTextBoxBehavior.RemoveQuoteStyle(p);
                    }
                    else
                    {
                        SlackRichTextBoxBehavior.ApplyQuoteStyle(p);
                    }
                }
                SlackRichTextBoxBehavior.NormalizeDocument(tbMessage);
            }
            finally
            {
                tbMessage.EndChange();
            }

            _vm.FocusTextBox = true;
        }

        private async void InlineCode_Click(object sender, RoutedEventArgs e)
        {
            await MainWindow.ShowDialog(string.Empty, "Ця функція буде доступна у майбутній версії.");
            _vm.FocusTextBox = true;
        }

        private async void CodeBlock_Click(object sender, RoutedEventArgs e)
        {
            await MainWindow.ShowDialog(string.Empty, "Ця функція буде доступна у майбутній версії.");
            _vm.FocusTextBox = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _vm.FocusTextBox = true;
        }

        private static IEnumerable<Paragraph> GetSelectedParagraphs(RichTextBox richTextBox)
        {
            var start = richTextBox.Selection.Start;
            var end = richTextBox.Selection.End;

            var current = start.Paragraph;
            var last = end.Paragraph;

            if (current == null)
                yield break;

            while (current != null)
            {
                yield return current;

                if (current == last)
                    yield break;

                current = current.NextBlock as Paragraph;
            }
        }

        private string? AskForUrl()
        {
            return Microsoft.VisualBasic.Interaction.InputBox(
                "Enter URL:",
                "Insert Link",
                "https://");
        }

        private static string GetSelectedText(RichTextBox richTextBox)
        {
            return richTextBox.Selection?.Text?.Trim() ?? string.Empty;
        }

        private static Hyperlink? GetSelectedHyperlink(RichTextBox richTextBox)
        {
            var selection = richTextBox.Selection;
            if (selection == null)
                return null;

            var start = selection.Start;
            var end = selection.End;

            // 1) quick checks around the edges
            var edgeHit =
                FindParentHyperlink(start) ??
                FindParentHyperlink(end) ??
                FindParentHyperlink(start.GetNextInsertionPosition(LogicalDirection.Forward)) ??
                FindParentHyperlink(start.GetNextInsertionPosition(LogicalDirection.Backward)) ??
                FindParentHyperlink(end.GetNextInsertionPosition(LogicalDirection.Forward)) ??
                FindParentHyperlink(end.GetNextInsertionPosition(LogicalDirection.Backward));

            if (edgeHit != null)
                return edgeHit;

            // 2) robust scan through the range
            TextPointer? pointer = start;

            while (pointer != null && pointer.CompareTo(end) <= 0)
            {
                var candidate = FindParentHyperlink(pointer);
                if (candidate != null)
                    return candidate;

                pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
            }

            return null;
        }

        private static Hyperlink? FindParentHyperlink(TextPointer? pointer)
        {
            if (pointer == null)
                return null;

            TextElement? current = pointer.Parent as TextElement;

            while (current != null)
            {
                if (current is Hyperlink hyperlink)
                    return hyperlink;

                current = current.Parent as TextElement;
            }

            return null;
        }

        private static void RemoveHyperlink(Hyperlink hyperlink)
        {
            if (hyperlink.Parent is not Paragraph paragraph)
                return;

            var inlines = hyperlink.Inlines.ToList();

            foreach (var inline in inlines)
                hyperlink.Inlines.Remove(inline);

            Inline? insertAfter = hyperlink;

            foreach (var inline in inlines)
            {
                paragraph.Inlines.InsertAfter(insertAfter, inline);
                insertAfter = inline;
            }

            paragraph.Inlines.Remove(hyperlink);
        }

        public sealed class EditLinkResult
        {
            public string? Url { get; set; }
            public bool Remove { get; set; }
        }

        private static void RefreshHyperlinkToolTips(Block block)
        {
            switch (block)
            {
                case Paragraph paragraph:
                    foreach (var inline in paragraph.Inlines)
                    {
                        RefreshHyperlinkToolTips(inline);
                    }
                    break;

                case Section section:
                    foreach (var child in section.Blocks)
                    {
                        RefreshHyperlinkToolTips(child);
                    }
                    break;

                case List list:
                    foreach (var item in list.ListItems)
                    {
                        foreach (var child in item.Blocks)
                        {
                            RefreshHyperlinkToolTips(child);
                        }
                    }
                    break;
            }
        }

        private static void RefreshHyperlinkToolTips(Inline inline)
        {
            switch (inline)
            {
                case Hyperlink hyperlink:
                    hyperlink.ToolTip = hyperlink.NavigateUri?.AbsoluteUri;
                    foreach (var child in hyperlink.Inlines)
                    {
                        RefreshHyperlinkToolTips(child);
                    }
                    break;

                case Span span:
                    foreach (var child in span.Inlines)
                    {
                        RefreshHyperlinkToolTips(child);
                    }
                    break;
            }
        }

        private void TbMessage_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not RichTextBox rtb)
                return;

            var point = e.GetPosition(rtb);
            var pointer = rtb.GetPositionFromPoint(point, true);
            var hyperlink = FindParentHyperlink(pointer);

            if (hyperlink?.NavigateUri != null)
            {
                var url = hyperlink.NavigateUri.AbsoluteUri;

                // Reopen only if hovered hyperlink changed
                if (!ReferenceEquals(_hoveredHyperlink, hyperlink))
                {
                    _hoveredHyperlink = hyperlink;

                    _linkToolTip.IsOpen = false;
                    _linkToolTip.Content = url;
                    _linkToolTip.IsOpen = true;
                }

                rtb.Cursor = Cursors.Hand;
            }
            else
            {
                HideLinkToolTip(rtb);
            }
        }

        private void TbMessage_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is not RichTextBox rtb)
                return;

            HideLinkToolTip(rtb);
        }

        private void HideLinkToolTip(RichTextBox rtb)
        {
            _hoveredHyperlink = null;
            _linkToolTip.IsOpen = false;
            rtb.Cursor = Cursors.IBeam;
        }
    }
}
