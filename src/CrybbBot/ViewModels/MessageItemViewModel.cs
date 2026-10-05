using System;
using System.Windows;
using System.Windows.Documents;

namespace CrybbBot.ViewModels
{
    public class MessageItemViewModel : ModelBase
    {
        public Guid ID { get; set; }

        //private string? _message;
        //public string? Message
        //{
        //    get => string.IsNullOrWhiteSpace(_message) || _message.Length <= 250 ? _message : _message.Substring(0, 250) + "...";
        //    set
        //    {
        //        _message = value;
        //        RaisePropertyChanged();
        //    }
        //}

        public string? Message
        {
            get
            {
                if (MsgBodyRich == null)
                {
                    return null;
                }
                return new TextRange(
                    MsgBodyRich.ContentStart,
                    MsgBodyRich.ContentEnd).Text;
            }
        }

        private FlowDocument? _msgBodyRich = new(new Paragraph());
        public FlowDocument? MsgBodyRich
        {
            get => _msgBodyRich;
            set
            {
                _msgBodyRich = value;
                RaisePropertyChanged();
                RaisePropertyChanged("Message");
            }
        }

        private int _attachmentsNumber;
        public int AttachmentsNumber
        {
            get => _attachmentsNumber;
            set
            {
                _attachmentsNumber = value;
                RaisePropertyChanged();
                RaisePropertyChanged("AttachmentsVisibility");
            }
        }
        public Visibility AttachmentsVisibility => AttachmentsNumber > 0 ? Visibility.Visible : Visibility.Collapsed;

        private string? _recipients;
        public string? Recipients
        {
            get => _recipients;
            set
            {
                _recipients = value;
                RaisePropertyChanged();
            }
        }

        private DateTime _insertDt;
        public DateTime InsertDT
        {
            get => _insertDt;
            set
            {
                _insertDt = value;
                RaisePropertyChanged();
                RaisePropertyChanged("InsertDTStr");
            }
        }
        public string InsertDTStr => InsertDT.ToString("dd/MM/yyyy   HH:mm");

        private DateTime? _sendingDt;
        public DateTime? SendingDT
        {
            get => _sendingDt;
            set
            {
                _sendingDt = value;
                RaisePropertyChanged();
                RaisePropertyChanged("SendingDTStr");
                RaisePropertyChanged("SendingDTVisibility");
            }
        }
        public string SendingDTStr => SendingDT.HasValue
            ? SendingDT.Value.ToString("dd/MM/yyyy   HH:mm")
            : string.Empty;
        public Visibility SendingDTVisibility => SendingDT.HasValue ? Visibility.Visible : Visibility.Collapsed;

        private DateTime? _scheduledDT;
        public DateTime? ScheduledDT
        {
            get => _scheduledDT;
            set
            {
                _scheduledDT = value;
                RaisePropertyChanged();
                RaisePropertyChanged("ScheduledDTStr");
                RaisePropertyChanged("ScheduledDTVisibility");
            }
        }
        public string ScheduledDTStr => ScheduledDT.HasValue
            ? ScheduledDT.Value.ToString("dd/MM/yyyy   HH:mm")
            : string.Empty;
        public Visibility ScheduledDTVisibility => ScheduledDT.HasValue ? Visibility.Visible : Visibility.Collapsed;
    }
}
