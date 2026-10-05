using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using CrybbBot.Behavior;
using CrybbBot.Helpers;
using CrybbBot.ViewModels;

namespace CrybbBot.Views
{
    /// <summary>
    /// Interaction logic for MessageItemView.xaml
    /// </summary>
    public partial class MessageItemView : UserControl
    {
        private MessageItemViewModel _vm => (MessageItemViewModel)Resources["ViewModel"];
        
        public MessageItemViewModel ViewModel => _vm;

        private ToolTip _linkToolTip { get; } = new();
        private Hyperlink? _hoveredHyperlink;

        public EventHandler? EditClicked { get; set; }
        public EventHandler? DeleteClicked { get; set; }

        public MessageItemView()
        {
            InitializeComponent();

            _linkToolTip.PlacementTarget = tbMessage;
            _linkToolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
            _linkToolTip.StaysOpen = true;
        }

        public MessageItemView(Models.Message message, List<DataLayer.Models.AttachedBundle>? bundles, List<DataLayer.Models.AttachedChannel>? channels)
        {
            InitializeComponent();

            _linkToolTip.PlacementTarget = tbMessage;
            _linkToolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
            _linkToolTip.StaysOpen = true;

            _vm.ID = message.ID;
            //_vm.Message = message.Text;
            _vm.AttachmentsNumber = message.AttachmentsNumber;
            _vm.InsertDT = message.InsertDT;
            _vm.ScheduledDT = message.ScheduleDT;
            _vm.SendingDT = message.SendingDT;

            try
            {
                _vm.MsgBodyRich = RichTextStorageHelper.FromXaml(message.Text);
            }
            catch
            {
                _vm.MsgBodyRich = RichTextStorageHelper.BuildPreview(message.Text);
            }

            var recepients = new List<string>();

            bundles = bundles?.Where(p => p.Bundle != null)?.ToList();
            if (bundles?.Any() ?? false)
            {
                var range = bundles.Select(p => string.IsNullOrWhiteSpace(p.Bundle!.Alias) ? p.Bundle.ID.ToString("D") : p.Bundle.Alias);
                recepients.AddRange(range);
            }

            channels = channels?.Where(p => p.Channel != null)?.ToList();
            if (channels?.Any() ?? false)
            {
                var range = channels.Select(p => string.IsNullOrWhiteSpace(p.Channel!.Alias) ? p.Channel.SlackChannelID : p.Channel.Alias);
                recepients.AddRange(range);
            }

            _vm.Recipients = string.Join(", ", recepients);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                tbMessage.Document = _vm.MsgBodyRich;
                SlackRichTextBoxBehavior.NormalizeDocument(tbMessage);
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void Edit_Click(object sender, System.Windows.RoutedEventArgs e) => EditClicked?.Invoke(this, e);

        private void Delete_Click(object sender, System.Windows.RoutedEventArgs e) => DeleteClicked?.Invoke(this, e);

        private void TbMessage_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not RichTextBox rtb)
                return;

            try
            {
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
            catch
            {
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
    }
}
