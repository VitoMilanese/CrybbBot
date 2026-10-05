using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using CrybbBot.Helpers;
using CrybbBot.Models;
using CrybbBot.Views;
using DataLayer;
using DataLayer.Models;
using SlackBotSender.Enums;
using SlackBotSender.Exceptions;

namespace CrybbBot.ViewModels
{
    public class NewMessageViewModel : ModelBase
    {
        public ObservableCollection<AttachmentView> Attachments { get; } = new ObservableCollection<AttachmentView>();
        public ObservableCollection<FileReference> References { get; } = new ObservableCollection<FileReference>();
        public ChannelMultiSelectComboBoxViewModel ChannelCombo { get; } = new();

        private DataLayer.Models.Message? _messageForEdit;
        public DataLayer.Models.Message? MessageForEdit
        {
            get => _messageForEdit;
            private set
            {
                _messageForEdit = value;
                RaisePropertyChanged("MessageForEdit");
                RaisePropertyChanged("AlreadySent");
                RaisePropertyChanged("SendingDT");
            }
        }

        public bool AlreadySent => MessageForEdit?.SendingDT.HasValue ?? false;

        public string SendingDT => AlreadySent
            ? MessageForEdit!.SendingDT!.Value.ToString("dd/MM/yyyy  HH:mm")
            : string.Empty;

        private static string _clipboardRepo { get; }

        private bool _focusTextBox;
        public bool FocusTextBox
        {
            get => _focusTextBox;
            set
            {
                _focusTextBox = value;
                RaisePropertyChanged();
            }
        }

        private bool _focusChannels;
        public bool FocusChannels
        {
            get => _focusChannels;
            set
            {
                _focusChannels = value;
                RaisePropertyChanged();
            }
        }

        public bool AreReferencesVisible => References.Any();

        public ICommand ScheduleCommand { get; }
        public ICommand SendNowCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand DeleteMessageCommand { get; }

        static NewMessageViewModel()
        {
            var oAssembly = Assembly.GetExecutingAssembly();
            var root = Path.GetDirectoryName(oAssembly.Location)!;
            _clipboardRepo = Path.Combine(root, "Clipboard");

            Task.Run(ClearClipboardRepo);
        }

        public NewMessageViewModel()
        {
            ScheduleCommand = new RelayCommand(Schedule);
            SendNowCommand = new RelayCommand(SendNow);
            ClearCommand = new RelayCommand(Clear);
            DeleteMessageCommand = new RelayCommand(DeleteMessage);

            LoadChannels();
        }

        public void LoadChannels()
        {
            ChannelCombo.Items.Clear();
            if (ChannelBundleViewModel.GlobalCollection?.Any() ?? false)
            {
                ChannelBundleViewModel? all = null;
                foreach (var bundle in ChannelBundleViewModel.GlobalCollection)
                {
                    if (all == null &&
                        bundle.Text.Equals(Constants.All, StringComparison.InvariantCultureIgnoreCase))
                    {
                        all = bundle;
                    }
                    else if (bundle.Channels.Any())
                    {
                        ChannelCombo.Items.Add(new ChannelItem(bundle));
                    }
                }

                if (all != null)
                {
                    if (all.Channels.Any())
                    {
                        ChannelCombo.Items.Insert(0, new ChannelItem(all, true));
                        foreach (var channel in all.Channels)
                        {
                            ChannelCombo.Items.Add(new ChannelItem(channel));
                        }
                    }
                }
            }
        }

        public async Task<bool> StartEditing(Guid messageId)
        {
            try
            {
                await ClearAsync();
                var message = await DbContext.Data.GetMessage(messageId);
                if (message != null)
                {
                    MessageForEdit = message;

                    //MsgBody = message.Text;
                    try
                    {
                        MsgBodyRich = RichTextStorageHelper.FromXaml(message.Text);
                    }
                    catch
                    {
                        MsgBodyRich = RichTextStorageHelper.BuildPreview(message.Text);
                    }

                    if (message.ScheduleDT.HasValue)
                    {
                        SelectedDate = message.ScheduleDT.Value.Date;
                        SelectedTime = message.ScheduleDT.Value;
                    }

                    if (message.Attachments?.Any() ?? false)
                    {
                        if (message.Attachments.Any(p => !string.IsNullOrWhiteSpace(p.ContentText) || p.ContentBin != null) && !Directory.Exists(_clipboardRepo))
                        {
                            Directory.CreateDirectory(_clipboardRepo);
                        }

                        Attachments.Clear();
                        References.Clear();
                        foreach (var attachment in message.Attachments)
                        {
                            switch (attachment.Type)
                            {
                                case DataLayer.Enums.AttachmentType.Image:
                                case DataLayer.Enums.AttachmentType.File:
                                    {
                                        if (string.IsNullOrWhiteSpace(attachment.Path))
                                        {
                                            continue;
                                        }

                                        var path = GetAttachedFileLocalPath(attachment.Path);

                                        if (!string.IsNullOrWhiteSpace(attachment.ContentText))
                                        {
                                            File.WriteAllText(path, attachment.ContentText);
                                        }
                                        else if (attachment.ContentBin != null)
                                        {
                                            File.WriteAllBytes(path, attachment.ContentBin);
                                        }
                                        else
                                        {
                                            continue;
                                        }

                                        var att = new AttachmentView(path, attachment.Path);
                                        //att.UpdateFilePath(attachment.Path);
                                        Attachments.Add(att);
                                    }
                                    break;
                                case DataLayer.Enums.AttachmentType.FileReference:
                                    {
                                        References.Add(new FileReference
                                        {
                                            Title = attachment.Title,
                                            Url = attachment.Path
                                        });
                                    }
                                    break;
                                default: continue;
                            }
                        }
                    }

                    var anyBundle = message.Bundles?.Any() ?? false;
                    var anyChannel = message.Channels?.Any() ?? false;
                    if (anyBundle || anyChannel)
                    {
                        ChannelCombo.SelectedItems.Clear();
                    }

                    if (anyBundle)
                    {
                        var remaining = message.Bundles!.Count;
                        foreach (var item in ChannelCombo.Items.Where(p => p.Bundle?.DbBundle != null))
                        {
                            if (message.Bundles!.Any(p => p.BundleID.Equals(item.Bundle!.DbBundle!.ID)))
                            {
                                ChannelCombo.SelectedItems.Add(item);
                                --remaining;
                            }
                            if (remaining == 0)
                            {
                                break;
                            }
                        }
                    }

                    if (anyChannel)
                    {
                        var remaining = message.Channels!.Count;
                        foreach (var item in ChannelCombo.Items.Where(p => p.Channel?.DbChannel != null))
                        {
                            if (message.Channels!.Any(p => p.ChannelID.Equals(item.Channel!.DbChannel!.ID)))
                            {
                                ChannelCombo.SelectedItems.Add(item);
                                --remaining;
                            }
                            if (remaining == 0)
                            {
                                break;
                            }
                        }
                    }

                    RaisePropertyChanged("AreReferencesVisible");
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static string GetAttachedFileLocalPath(string filePath)
        {
            var repo = Path.GetFullPath(_clipboardRepo);
            filePath = Path.GetFullPath(filePath);

            if (filePath.StartsWith(repo, StringComparison.InvariantCultureIgnoreCase))
            {
                var dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir!);
                }
                return Path.Combine(dir!, Path.GetFileName(filePath));
            }
            else
            {
                return Path.Combine(repo, Path.GetFileName(filePath));
            }
        }

        //private string? _msgBody;
        //public string? MsgBody
        //{
        //    get => _msgBody;
        //    set
        //    {
        //        _msgBody = value;
        //        RaisePropertyChanged();
        //        RaisePropertyChanged("CanSend");
        //    }
        //}

        public string? MsgBody
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

        private FlowDocument? _msgBodyRich = RichTextStorageHelper.BuildPreview(string.Empty);
        public FlowDocument? MsgBodyRich
        {
            get => _msgBodyRich;
            set
            {
                _msgBodyRich = value;
                RaisePropertyChanged();
                RaisePropertyChanged("MsgBody");
                RaisePropertyChanged("CanSend");

                if (RichTextBoxToBeUpdated != null)
                {
                    RichTextBoxToBeUpdated.Document = _msgBodyRich;
                }
            }
        }

        public RichTextBox? RichTextBoxToBeUpdated { get; set; }

        public bool CanSend => !string.IsNullOrWhiteSpace(MsgBody?.Trim());

        private DateTime? _selectedDate;
        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                RaisePropertyChanged();
            }
        }

        private DateTime? _selectedTime;
        public DateTime? SelectedTime
        {
            get => _selectedTime;
            set
            {
                _selectedTime = value;
                RaisePropertyChanged();
            }
        }

        public async void Clear() => await ClearAsync();

        public async Task ClearAsync()
        {
            MessageForEdit = null;
            //MsgBody = null;
            MsgBodyRich = RichTextStorageHelper.BuildPreview(string.Empty);
            SelectedDate = null;
            SelectedTime = null;
            Attachments.Clear();
            References.Clear();
            ChannelCombo.ClearAllCommand.Execute(null);
            RaisePropertyChanged("AreReferencesVisible");
            RaisePropertyChanged("MessageForEdit");
            RaisePropertyChanged("AlreadySent");
            RaisePropertyChanged("SendingDT");

            await Task.Run(ClearClipboardRepo);

            FocusTextBox = true;
        }

        public async void DeleteMessage()
        {
            if (MessageForEdit == null)
            {
                return;
            }

            var result = await MainWindow.ShowYesNoDialog(string.Empty, "Ви впевнені, що бажаєте видалити це повідомлення?");

            if (result)
            {
                try
                {
                    await DbContext.Data.DeleteMessage(MessageForEdit.ID);
                }
                catch
                {
                    await MainWindow.ShowDialog(string.Empty, "При видаленні повідомлення з бази даних виникла помилка");
                }

                Clear();

                MainWindow.ShowAllMessages();
            }
        }

        private async void Schedule()
        {
            await ManageMessage(true);
        }

        private async void SendNow()
        {
            await ManageMessage();
        }

        private async Task ManageMessage(bool schedule = false)
        {
            if (schedule)
            {
                if (!SelectedDate.HasValue)
                {
                    await MainWindow.ShowDialog(string.Empty, "Для планування потрібно обрати дату");
                    return;
                }

                if (!SelectedTime.HasValue)
                {
                    await MainWindow.ShowDialog(string.Empty, "Для планування потрібно обрати час");
                    return;
                }
            }

            var startBot = false;
            var saved = false;
            var dbBundlesDict = new Dictionary<AttachedBundle, List<DataLayer.Models.Channel>>();
            var appChannels = new List<DataLayer.Models.Channel>();
            var chs = (string[]?)null;
            var messageBody = MsgBody;
            object? block = null;
            try
            {
                await HeavyTaskManager.DoHeavyWorkAsync("Повідомлення відправляється...", async () =>
                {
                    if (string.IsNullOrWhiteSpace(messageBody))
                    {
                        throw new MessageNotSpecifiedException();
                    }

                    block = SlackRichTextBlockConverter.ToSlackRichTextBlockObject(MsgBodyRich);

                    List<string>? channels = null;
                    if (ChannelCombo.SelectedItems.Any())
                    {
                        channels = new List<string>();
                        foreach (var item in ChannelCombo.SelectedItems)
                        {
                            if (item.Channel != null)
                            {
                                channels.Add(item.Channel.Id);
                                appChannels.Add(item.Channel.DbChannel!);
                            }
                            else if (item.Bundle != null)
                            {
                                AttachedBundle? dbBundle = null;
                                if (item.Bundle.DbBundle != null)
                                {
                                    dbBundle = new AttachedBundle
                                    {
                                        ID = Guid.NewGuid(),
                                        Bundle = item.Bundle.DbBundle,
                                        BundleID = item.Bundle.DbBundle.ID
                                    };
                                    dbBundlesDict.Add(dbBundle, new List<DataLayer.Models.Channel>());
                                }
                                else
                                {
                                    // all
                                    dbBundle = dbBundlesDict.FirstOrDefault(p => p.Key.ID.Equals(Guid.Empty)).Key;
                                    if (dbBundle == null)
                                    {
                                        dbBundle = new AttachedBundle
                                        {
                                            ID = Guid.Empty,
                                            Bundle = null,
                                            BundleID = Guid.Empty
                                        };
                                        dbBundlesDict.Add(dbBundle, new List<DataLayer.Models.Channel>());
                                    }
                                }
                                if (!item.Bundle.Channels.Any() && dbBundle != null)
                                {
                                    dbBundlesDict.Remove(dbBundle);
                                }
                                else
                                {
                                    foreach (var channel in item.Bundle.Channels)
                                    {
                                        channels.Add(channel.Id);
                                        dbBundlesDict[dbBundle!].Add(channel.DbChannel!);
                                    }
                                }
                            }
                            else
                            {
                                //channels.Add(item.Name);
                            }
                        }
                    }

                    List<SlackBotSender.Models.Attachment>? attachments = null;

                    var hasAttachments = Attachments.Any();
                    var hasReferences = References.Any();

                    if (hasAttachments || hasReferences)
                    {
                        attachments = new List<SlackBotSender.Models.Attachment>();
                    }

                    if (hasAttachments)
                    {
                        foreach (var attachment in Attachments.Where(p => !string.IsNullOrWhiteSpace(p.FilePath) && File.Exists(p.FilePath)))
                        {
                            attachments!.Add(new SlackBotSender.Models.Attachment
                            {
                                Type = attachment.IsImage
                                    ? AttachmentType.Image
                                    : AttachmentType.File,
                                FilePath = attachment.FilePath!,
                                Title = string.IsNullOrWhiteSpace(attachment.OriginalFilePath) ||
                                        attachment.OriginalFilePath.Equals(attachment.FilePath, StringComparison.InvariantCultureIgnoreCase)
                                    ? string.Empty
                                    : Path.GetFileNameWithoutExtension(attachment.OriginalFilePath),
                            });
                        }
                    }

                    if (hasReferences)
                    {
                        foreach (var reference in References.Where(p => !string.IsNullOrWhiteSpace(p.Url)))
                        {
                            attachments!.Add(new SlackBotSender.Models.Attachment
                            {
                                Type = AttachmentType.FileReference,
                                FilePath = reference.Url!,
                                Title = reference.Title ?? string.Empty
                            });
                        }
                    }

                    chs = channels?.Distinct()?.ToArray();

                    var att = attachments?.ToArray();
                    
                    var bodyXaml = RichTextStorageHelper.ToXaml(MsgBodyRich);

                    if (schedule)
                    {
                        // Schedule
                        var scheduleDt = SelectedDate!.Value.AddHours(SelectedTime!.Value.Hour).AddMinutes(SelectedTime.Value.Minute);
                        
                        if (MessageForEdit == null)
                        {
                            try
                            {
                                await RegisterScheduledMessageInDb(bodyXaml, scheduleDt, appChannels, dbBundlesDict, att);
                            }
                            catch (Exception ex)
                            {
                                throw new RegisterMessageInDbException(ex);
                            }
                        }
                        else
                        {
                            // UPDATE
                            try
                            {
                                await UpdateScheduledMessageInDb(bodyXaml, scheduleDt, null, appChannels, dbBundlesDict, att);
                                saved = true;
                            }
                            catch (Exception ex)
                            {
                                throw new RegisterMessageInDbException(ex);
                            }
                        }
                    }
                    else
                    {
                        // Send now
                        //var result = await SlackBotSender.Client.SendMessage(messageBody, chs, att);
                        var result = await SlackBotSender.Client.SendMessage(block, chs, att);

                        if (MessageForEdit == null)
                        {
                            // INSERT
                            try
                            {
                                await RegisterSentMessageInDb(bodyXaml, DateTime.Now, appChannels, dbBundlesDict, result, att);
                            }
                            catch (Exception ex)
                            {
                                throw new RegisterMessageInDbException(ex);
                            }
                        }
                        else
                        {
                            // UPDATE
                            try
                            {
                                await UpdateScheduledMessageInDb(bodyXaml, null, DateTime.Now, appChannels, dbBundlesDict, att);
                                // TODO: Save errors to DB
                                saved = true;
                            }
                            catch (Exception ex)
                            {
                                throw new RegisterMessageInDbException(ex);
                            }
                        }
                    }

                    await ClearAsync();

                    FocusTextBox = true;
                });
            }
            #region Catch
            catch (InvalidOperationException)
            {
                await MainWindow.ShowDialog(string.Empty, "Помилка аутентифікації\r\nПеревірте налаштування параметра Bot User OAuth Token.");
                return;
            }
            catch (ChannelNotFoundException ex)
            {
                await MainWindow.ShowDialog(string.Empty, $"Помилка\r\nКанал '{ex.ChannelId}' не існує.");
                return;
            }
            catch (ClientNotInitializedException)
            {
                startBot = true;
            }
            catch (MessageNotSpecifiedException)
            {
                await MainWindow.ShowDialog("Помилка", "Не задано текст повідомлення");
                FocusTextBox = true;
            }
            catch (ChannelNotSpecifiedException)
            {
                await MainWindow.ShowDialog("Помилка", "Не вказано жодного каналу");
                FocusChannels = true;
            }
            catch (RegisterMessageInDbException ex)
            {
                if (schedule)
                {
                    await MainWindow.ShowDialog(ex.Message, "Під час збереження повідомлення у базі даних\r\nвиникла помилка.");
                }
                else
                {
                    await MainWindow.ShowDialog(ex.Message, "Повідомлення відправлено, але під час його\r\nзбереження у базі даних виникла помилка.");
                }
            }
            catch (Exception ex)
            {
                await MainWindow.ShowDialog("Помилка", ex.Message);
            }
            #endregion

            if (!schedule)
            {
                if (startBot)
                {
                    try
                    {
                        startBot = await MainWindow.ShowYesNoDialog(string.Empty, "Бот не запущений.\r\nБажаєте його запустити?");
                    }
                    catch (ClientNotInitializedException)
                    {
                        await MainWindow.ShowDialog("Помилка", "Бот не запущений");
                    }
                    catch (ClientAlreadyInitializedException)
                    {
                        await MainWindow.ShowDialog("Помилка", "Бот вже запущений");
                    }
                    catch (Exception ex)
                    {
                        await MainWindow.ShowDialog("Помилка", ex.Message);
                    }
                }

                if (startBot)
                {
                    startBot = await MainWindow.ManageBotState(Enums.BotStatus.Running);

                    if (startBot)
                    {
                        SendNow();
                    }
                }
            }
            else if (saved)
            {
                MainWindow.ShowAllMessages();
            }
        }

        private async Task RegisterScheduledMessageInDb(
            string messageBody,
            DateTime? scheduleDt,
            List<DataLayer.Models.Channel> appChannels,
            Dictionary<AttachedBundle, List<DataLayer.Models.Channel>> dbBundlesDict,
            SlackBotSender.Models.Attachment[]? attachments)
        {
            var message = new DataLayer.Models.Message
            {
                Text = messageBody,
                InsertDT = DateTime.Now,
                SendingDT = null,
                Managed = false,
                ScheduleDT = scheduleDt
            };
            await DbContext.Data.AddMessage(message);

            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    var text = (string?)null;
                    var bin = (byte[]?)null;

                    if (attachment.Type == AttachmentType.File || attachment.Type == AttachmentType.Image)
                    {
                        if (!File.Exists(attachment.FilePath))
                        {
                            continue;
                        }

                        try
                        {
                            bin = File.ReadAllBytes(attachment.FilePath);

                            if (attachment.Type != AttachmentType.Image && Common.IsTextFile(bin))
                            {
                                try
                                {
                                    text = File.ReadAllText(attachment.FilePath);
                                    bin = null;
                                }
                                catch
                                {
                                    if (bin == null && text == null)
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    var dbAttachment = new AttachedAttachment
                    {
                        MessageID = message.ID,
                        Type = (DataLayer.Enums.AttachmentType)attachment.Type,
                        Path = attachment.FilePath,
                        Title = attachment.Title,
                        FieldID = attachment.FieldId,
                        ContentText = text,
                        ContentBin = bin
                    };

                    await DbContext.Data.AddAttachment(dbAttachment);
                }
            }

            foreach (var channel in appChannels)
            {
                var attachedChannel = new AttachedChannel
                {
                    Channel = channel,
                    ChannelID = channel.ID,
                    Message = message,
                    MessageID = message.ID
                };
                await DbContext.Data.AddAttachedChannel(attachedChannel);
                if (message.Channels == null)
                {
                    message.Channels = new List<AttachedChannel>();
                }
                message.Channels.Add(attachedChannel);
            }

            foreach (var bundle in dbBundlesDict.Keys)
            {
                var attachedBundle = new AttachedBundle
                {
                    Bundle = bundle.Bundle,
                    BundleID = bundle.BundleID,
                    Message = message,
                    MessageID = message.ID
                };
                await DbContext.Data.AddAttachedBundle(attachedBundle);
                if (message.Bundles == null)
                {
                    message.Bundles = new List<AttachedBundle>();
                }
                message.Bundles.Add(attachedBundle);
            }
        }
        
        private async Task UpdateScheduledMessageInDb(
            string messageBody,
            DateTime? scheduleDt,
            DateTime? sendingDt,
            List<DataLayer.Models.Channel> appChannels,
            Dictionary<AttachedBundle, List<DataLayer.Models.Channel>> dbBundlesDict,
            SlackBotSender.Models.Attachment[]? attachments)
        {
            var message = new DataLayer.Models.Message
            {
                ID = MessageForEdit!.ID,
                Text = messageBody,
                InsertDT = DateTime.Now,
                SendingDT = null,
                Managed = true,
                ScheduleDT = scheduleDt
            };

            if (appChannels?.Any() ?? false)
            {
                message.Channels = new List<AttachedChannel>();

                foreach (var channel in appChannels)
                {
                    message.Channels.Add(new AttachedChannel
                    {
                        Channel = channel,
                        ChannelID = channel.ID,
                        Message = message,
                        MessageID = message.ID
                    });
                }
            }

            if (dbBundlesDict?.Any() ?? false)
            {
                message.Bundles = new List<AttachedBundle>();

                foreach (var bundle in dbBundlesDict.Keys.Where(p => p.Bundle != null))
                {
                    message.Bundles.Add(new AttachedBundle
                    {
                        Bundle = bundle.Bundle,
                        BundleID = bundle.Bundle!.ID,
                        Message = message,
                        MessageID = message.ID
                    });
                }
            }

            if (attachments?.Any() ?? false)
            {
                message.Attachments = new List<AttachedAttachment>();

                foreach (var attachment in attachments)
                {
                    var text = (string?)null;
                    var bin = (byte[]?)null;

                    if (attachment.Type == AttachmentType.File || attachment.Type == AttachmentType.Image)
                    {
                        if (!File.Exists(attachment.FilePath))
                        {
                            continue;
                        }

                        try
                        {
                            bin = File.ReadAllBytes(attachment.FilePath);

                            if (attachment.Type != AttachmentType.Image && Common.IsTextFile(bin))
                            {
                                try
                                {
                                    text = File.ReadAllText(attachment.FilePath);
                                    bin = null;
                                }
                                catch
                                {
                                    if (bin == null && text == null)
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    message.Attachments.Add(new AttachedAttachment
                    {
                        Message = message,
                        MessageID = message.ID,
                        Type = (DataLayer.Enums.AttachmentType)attachment.Type,
                        Path = attachment.FilePath,
                        Title = attachment.Title,
                        FieldID = attachment.FieldId,
                        ContentText = text,
                        ContentBin = bin
                    });
                }
            }

            await DbContext.Data.UpdateMessage(message);
        }

        private async Task RegisterSentMessageInDb(
            string messageBody,
            DateTime? sendingDt,
            List<DataLayer.Models.Channel> appChannels,
            Dictionary<AttachedBundle, List<DataLayer.Models.Channel>> dbBundlesDict,
            List<SlackBotSender.Models.SendMessageResult> result,
            SlackBotSender.Models.Attachment[]? attachments)
        {
            var message = new DataLayer.Models.Message
            {
                Text = messageBody,
                InsertDT = DateTime.Now,
                SendingDT = sendingDt,
                Managed = true,
                ScheduleDT = null
            };
            //await SqlServerBootstrap.AddMessage(message);
            await DbContext.Data.AddMessage(message);

            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    var text = (string?)null;
                    var bin = (byte[]?)null;

                    if (attachment.Type == AttachmentType.File || attachment.Type == AttachmentType.Image)
                    {
                        if (!File.Exists(attachment.FilePath))
                        {
                            continue;
                        }

                        try
                        {
                            bin = File.ReadAllBytes(attachment.FilePath);
                            
                            if (attachment.Type != AttachmentType.Image && Common.IsTextFile(bin))
                            {
                                try
                                {
                                    text = File.ReadAllText(attachment.FilePath);
                                    bin = null;
                                }
                                catch
                                {
                                    if (bin == null && text == null)
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    var dbAttachment = new AttachedAttachment
                    {
                        MessageID = message.ID,
                        Type = (DataLayer.Enums.AttachmentType)attachment.Type,
                        Path = attachment.FilePath,
                        Title = attachment.Title,
                        FieldID = attachment.FieldId,
                        ContentText = text,
                        ContentBin = bin
                    };

                    await DbContext.Data.AddAttachment(dbAttachment);
                }
            }

            foreach (var r in result.Where(p => p.Result != SendMessageResult.None))
            {
                var appChannel = appChannels.FirstOrDefault(p => p.SlackChannelID.Equals(r.ChannelId));
                if (appChannel != null && (message.Channels == null || !message.Channels.Any(p => p.ChannelID.Equals(appChannel.ID))))
                {
                    // Channel
                    var attachedChannel = new AttachedChannel
                    {
                        Channel = appChannel,
                        ChannelID = appChannel.ID,
                        Message = message,
                        MessageID = message.ID
                    };
                    //await SqlServerBootstrap.AddAttachedChannel(attachedChannel);
                    await DbContext.Data.AddAttachedChannel(attachedChannel);
                    if (message.Channels == null)
                    {
                        message.Channels = new List<AttachedChannel>();
                    }
                    message.Channels.Add(attachedChannel);
                }

                if (appChannel == null)
                {
                    var dbBundle = dbBundlesDict.FirstOrDefault(p => p.Value.Any(p => p.SlackChannelID.Equals(r.ChannelId))).Key;
                    if (dbBundle != null)
                    {
                        if (dbBundle.BundleID.Equals(Guid.Empty))
                        {
                            // All
                            appChannel = dbBundlesDict[dbBundle].FirstOrDefault(p => p.SlackChannelID.Equals(r.ChannelId));
                            if (appChannel != null && (message.Channels == null || !message.Channels.Any(p => p.ChannelID.Equals(appChannel.ID))))
                            {
                                var attachedChannel = new AttachedChannel
                                {
                                    Channel = appChannel,
                                    ChannelID = appChannel.ID,
                                    Message = message,
                                    MessageID = message.ID
                                };
                                //await SqlServerBootstrap.AddAttachedChannel(attachedChannel);
                                await DbContext.Data.AddAttachedChannel(attachedChannel);
                                if (message.Channels == null)
                                {
                                    message.Channels = new List<AttachedChannel>();
                                }
                                message.Channels.Add(attachedChannel);
                            }
                        }
                        else
                        {
                            // Bundle
                            appChannel = dbBundlesDict[dbBundle].FirstOrDefault(p => p.SlackChannelID.Equals(r.ChannelId));

                            if (message.Bundles == null || !message.Bundles.Any(p => p.BundleID.Equals(dbBundle.BundleID)))
                            {
                                var attachedBundle = new AttachedBundle
                                {
                                    Bundle = dbBundle.Bundle,
                                    BundleID = dbBundle.Bundle!.ID,
                                    Message = message,
                                    MessageID = message.ID
                                };
                                //await SqlServerBootstrap.AddAttachedBundle(attachedBundle);
                                await DbContext.Data.AddAttachedBundle(attachedBundle);
                                if (message.Bundles == null)
                                {
                                    message.Bundles = new List<AttachedBundle>();
                                }
                                message.Bundles.Add(attachedBundle);
                            }
                        }
                    }
                }

                if (r.Result == SendMessageResult.Failed)
                {
                    var dbError = new SendingError
                    {
                        Message = message,
                        MessageID = message.ID,
                        Channel = appChannel,
                        ChannelID = appChannel?.ID ?? Guid.Empty,
                        Details = r.Error,
                        Type = (int)r.ErrorType
                    };
                    //await SqlServerBootstrap.AddSendingError(dbError);
                    await DbContext.Data.AddSendingError(dbError);
                    if (message.SendingErrors == null)
                    {
                        message.SendingErrors = new List<SendingError>();
                    }
                    message.SendingErrors.Add(dbError);
                }
            }
        }

        private static async Task ClearClipboardRepo()
        {
            if (Directory.Exists(_clipboardRepo))
            {
                var files = Directory.GetFiles(_clipboardRepo, "*", SearchOption.AllDirectories)
                    .Where(p => p.StartsWith(_clipboardRepo, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();
                foreach (var file in files)
                {
                    var retry = 4;
                    while (retry > 0)
                    {
                        try
                        {
                            File.Delete(file);
                            retry = 0;
                        }
                        catch
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(500));
                            --retry;
                        }
                    }
                }

                var dirs = Directory.GetDirectories(_clipboardRepo, "*", SearchOption.TopDirectoryOnly)
                    .ToList();
                foreach (var dir in dirs)
                {
                    var retry = 4;
                    while (retry > 0)
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                            retry = 0;
                        }
                        catch
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(500));
                            --retry;
                        }
                    }
                }
            }
        }

        public void AddReference(FileReference reference)
        {
            References.Add(reference);
            RaisePropertyChanged("AreReferencesVisible");
        }

        public void RemoveReference(FileReference reference)
        {
            if (References.Contains(reference))
            {
                References.Remove(reference);
                RaisePropertyChanged("AreReferencesVisible");
            }
        }
    }
}
