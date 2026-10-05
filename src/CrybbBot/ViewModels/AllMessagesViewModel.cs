using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CrybbBot.Helpers;
using CrybbBot.Views;
using DataLayer;
using DataLayer.Models;

namespace CrybbBot.ViewModels
{
    public class AllMessagesViewModel : ModelBase
    {
        public ObservableCollection<MessageItemView> Messages { get; } = new();

        private MessagesFilter _searchFilter { get; set; } = MessagesFilter.Default;

        private DateTime? _selectedDate;
        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                _searchFilter.CreatedFrom = _selectedDate;
                RaisePropertyChanged();
            }
        }

        public bool IsFilterOrderAscending
        {
            get => _searchFilter.Ascending;
            set
            {
                _searchFilter.Ascending = value;
                RaisePropertyChanged();
                Refresh();
            }
        }

        public bool IsFilterAll
        {
            get => !(_searchFilter.Scheduled.HasValue && _searchFilter.Scheduled.Value) &&
                !(_searchFilter.Sent.HasValue && _searchFilter.Sent.Value) &&
                !(_searchFilter.Errors.HasValue && _searchFilter.Errors.Value);
            set
            {
                if (value)
                {
                    _searchFilter.Scheduled = null;
                    _searchFilter.Sent = null;
                    _searchFilter.Errors = null;
                    RaisePropertyChanged("IsFilterScheduled");
                    RaisePropertyChanged("IsFilterSent");
                    RaisePropertyChanged("IsFilterErrors");
                }
                RaisePropertyChanged();
                Refresh();
            }
        }

        public bool IsFilterScheduled
        {
            get => _searchFilter.Scheduled.HasValue && _searchFilter.Scheduled.Value;
            set
            {
                _searchFilter.Scheduled = value ? true : null;
                RaisePropertyChanged();
                RaisePropertyChanged("IsFilterAll");
                Refresh();
            }
        }

        public bool IsFilterSent
        {
            get => _searchFilter.Sent.HasValue && _searchFilter.Sent.Value;
            set
            {
                _searchFilter.Sent = value ? true : null;
                RaisePropertyChanged();
                RaisePropertyChanged("IsFilterAll");
                Refresh();
            }
        }

        public bool IsFilterErrors
        {
            get => _searchFilter.Errors.HasValue && _searchFilter.Errors.Value;
            set
            {
                _searchFilter.Errors = value ? true : null;
                RaisePropertyChanged();
                RaisePropertyChanged("IsFilterAll");
                Refresh();
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand CancelDateSelectionCommand { get; }
        public ICommand AdvancedFilterCommand { get; }

        public AllMessagesViewModel()
        {
            RefreshCommand = new RelayCommand(Refresh);
            CancelDateSelectionCommand = new RelayCommand(CancelDateSelection);
            AdvancedFilterCommand = new RelayCommand(AdvancedFilter);

            SelectedDate = DateTime.Now.AddMonths(-1);
            IsFilterAll = true;
        }

        public async void LoadFromDb()
        {
            Messages.Clear();
            try
            {
                var messages = await DbContext.Data.GetAllMessages(_searchFilter);
                foreach (var message in messages)
                {
                    List<AttachedAttachment>? attachments = new List<AttachedAttachment>();
                    try
                    {
                        var attachmentsNumber = await DbContext.Data.GetAttachmentsNumber(message.ID);
                        if (attachmentsNumber == 0)
                        {
                            attachments = null;
                        }
                        else
                        {
                            for (var i = 0; i < attachmentsNumber; ++i)
                            {
                                attachments.Add(new AttachedAttachment());
                            }
                            message.Attachments = attachments;
                        }
                    }
                    catch
                    {
                        //await MainWindow.ShowDialog("Помилка при занесенні до бази даних", ex.Message);
                    }

                    var model = new Models.Message
                    {
                        ID = message.ID,
                        Text = message.Text,
                        AttachmentsNumber = message.Attachments?.Count ?? 0,
                        InsertDT = message.InsertDT,
                        SendingDT = message.SendingDT,
                        ScheduleDT = message.ScheduleDT,
                        Sent = message.SendingDT.HasValue
                    };

                    var bundles = await DbContext.Data.GetAttachedBundles(message.ID);
                    if (bundles?.Any() ?? false) bundles.ForEach(p => p.Message = message);

                    var channels = await DbContext.Data.GetAttachedChannels(message.ID);
                    if (channels?.Any() ?? false) channels.ForEach(p => p.Message = message);

                    var messageItemView = new MessageItemView(model, bundles, channels);
                    Messages.Add(messageItemView);

                    messageItemView.EditClicked = EditMessage_OnClick;
                    messageItemView.DeleteClicked = DeleteMessage_OnClick;
                }
            }
            catch (Exception ex)
            {
                await MainWindow.ShowDialog("Помилка при зверненні до бази даних", ex.Message);
            }
        }

        private async void EditMessage_OnClick(object sender, EventArgs e)
        {
            if (sender is not MessageItemView view)
            {
                return;
            }

            await MainWindow.EditMessage(view.ViewModel.ID);
        }

        private async void DeleteMessage_OnClick(object sender, EventArgs e)
        {
            if (sender is not MessageItemView view)
            {
                return;
            }

            if (!Messages.Contains(view))
            {
                return;
            }

            var result = await MainWindow.ShowYesNoDialog(string.Empty, "Ви впевнені, що бажаєте видалити це повідомлення?");

            if (result)
            {
                try
                {
                    await DbContext.Data.DeleteMessage(view.ViewModel.ID);

                    try
                    {
                        Messages.Remove(view);
                    }
                    catch
                    {
                        await MainWindow.ShowDialog(string.Empty, "Повідомлення було видалено з бази даних,\r\nале при його видаленні з цього списку виникла помилка.\r\nБудь ласка, спробуйте обновити список.");
                    }
                }
                catch
                {
                    await MainWindow.ShowDialog(string.Empty, "При видаленні повідомлення з бази даних виникла помилка");
                }
            }
        }

        private void CancelDateSelection()
        {
            SelectedDate = null;
        }

        public async void Refresh()
        {
            if (!HeavyTaskManager.HasMainWindowViewModelSet)
            {
                return;
            }
            await HeavyTaskManager.DoHeavyWorkAsync("Завантаження...", async () =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10));
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    LoadFromDb();
                },
                System.Windows.Threading.DispatcherPriority.Normal);
            });
        }

        private async void AdvancedFilter()
        {
            var result = await MainWindow.ShowMessagesFilterDialog(_searchFilter);
            if (result.Yes && result.Filter != null)
            {
                _searchFilter = result.Filter;
                RaisePropertyChanged("IsFilterOrderAscending");
                SelectedDate = _searchFilter.CreatedFrom;
                Refresh();
            }
        }
    }
}
