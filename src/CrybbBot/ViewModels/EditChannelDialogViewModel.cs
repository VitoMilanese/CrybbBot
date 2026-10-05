using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CrybbBot.Helpers;
using MaterialDesignThemes.Wpf;

namespace CrybbBot.ViewModels;

public class EditChannelDialogViewModel : ModelBase
{
    private string _oldId;
    public string OldId
    {
        get => _oldId;
        set
        {
            _oldId = value;
            RaisePropertyChanged();
        }
    }

    private string? _newId;
    public string? NewId
    {
        get => _newId;
        set
        {
            _newId = value;
            RaisePropertyChanged();
        }
    }

    private string? _oldAlias;
    public string? OldAlias
    {
        get => _oldAlias;
        set
        {
            _oldAlias = value;
            RaisePropertyChanged();
        }
    }

    private string? _newAlias;
    public string? NewAlias
    {
        get => _newAlias;
        set
        {
            _newAlias = value;
            RaisePropertyChanged();
        }
    }

    private int _maxLength = 0;
    public int MaxLength
    {
        get => _maxLength;
        set
        {
            _maxLength = value;
            RaisePropertyChanged();
        }
    }

    private bool _focusNewId;
    public bool FocusNewId
    {
        get => _focusNewId;
        set
        {
            _focusNewId = value;
            RaisePropertyChanged();
        }
    }

    public ObservableCollection<ChannelInBundleWithFlagViewModel> Bundles { get; init; } = new ObservableCollection<ChannelInBundleWithFlagViewModel>();

    public ICommand DeleteFromAllBundlesCommand { get; private set; }
    public ICommand ResetBundlesCommand { get; private set; }

    public EditChannelRoutedEventInfo? EditChannelRoutedEvent { get; set; }

    public EditChannelDialogViewModel(string oldId, string? oldAlias) => Init(0, oldId, oldAlias);

    public EditChannelDialogViewModel(int maxLength, string oldId, string? oldAlias) => Init(maxLength, oldId, oldAlias);

    private void Init(int maxLength, string oldId, string? oldAlias)
    {
        (MaxLength, OldId, OldAlias) = (maxLength, oldId, oldAlias);

        DeleteFromAllBundlesCommand = new RelayCommand(DeleteFromAllBundles);
        ResetBundlesCommand = new RelayCommand(LoadBundles);

        LoadBundles();
    }

    private void LoadBundles()
    {
        if (ChannelBundleViewModel.GlobalCollection != null)
        {
            Bundles.Clear();
            var bundleList = new List<ChannelInBundleWithFlagViewModel>();

            foreach (var bundle in ChannelBundleViewModel.GlobalCollection)
            {
                var isPresent = false;

                foreach (var item in bundle.Channels)
                {
                    if (item.Id.Equals(OldId) || (!string.IsNullOrWhiteSpace(OldAlias) &&
                                               !string.IsNullOrWhiteSpace(item.PureAlias) &&
                                               OldAlias.Equals(item.PureAlias)))
                    {
                        isPresent = true;
                        break;
                    }
                }

                bundleList.Add(new ChannelInBundleWithFlagViewModel
                {
                    IsEnabled = !bundle.Text.Equals(Constants.All, System.StringComparison.InvariantCultureIgnoreCase),
                    IsPresent = isPresent,
                    WasPresent = isPresent,
                    BundleName = bundle.Text,
                });
            }

            foreach (var bundle in bundleList)
            {
                Bundles.Add(bundle);
            }

            RaisePropertyChanged("Bundles");
        }
    }

    private async void DeleteFromAllBundles()
    {
        DialogHost.CloseDialogCommand.Execute(
            parameter: false,
            target: null);

        var result = await MainWindow.ShowYesNoDialog(string.Empty, "Ви впевнені, що хочете видалити цей канал з усіх груп?");
        if (!result)
        {
            EditChannelRoutedEvent?.Click(this);
        }
        else
        {
            var msg = "Видалення каналу з усіх груп...";
            await HeavyTaskManager.DoHeavyWorkAsync(msg, async () =>
            {
                try
                {
                    //await Task.Delay(TimeSpan.FromSeconds(1));
                    await DeleteFromAllBundlesTask();
                }
                catch
                {
                }
            });
        }
    }

    private Task DeleteFromAllBundlesTask()
    {
        EditChannelRoutedEvent?.Dispose();

        if (ChannelBundleViewModel.GlobalCollection != null)
        {
            foreach (var bundle in ChannelBundleViewModel.GlobalCollection)
            {
                foreach (var item in bundle.Channels)
                {
                    if (item.Id.Equals(OldId) || (!string.IsNullOrWhiteSpace(OldAlias) &&
                                                  !string.IsNullOrWhiteSpace(item.PureAlias) &&
                                                  OldAlias.Equals(item.PureAlias)))
                    {
                        bundle.Channels.Remove(item);
                        break;
                    }
                }
            }

            var channel = ChannelManagementViewModel.FindChannelGlobally(OldId, OldAlias);
            if (channel != null)
            {
                ChannelManagementViewModel.DeleteChannelGlobally(channel);
            }
        }

        return Task.CompletedTask;
    }

    public class EditChannelRoutedEventInfo : IDisposable
    {
        public RoutedEventHandler? Event { get; set; }
        public object? Clicker { get; set; }

        public void Click(EditChannelDialogViewModel viewModel)
        {
            if (Event != null && Clicker != null)
            {
                Event.Invoke(Clicker, new EditChannelDialogRoutedEventArgs(viewModel));
            }
        }

        public void Dispose()
        {
            Event = null;
            Clicker = null;
        }
    }

    public class EditChannelDialogRoutedEventArgs : RoutedEventArgs
    {
        public EditChannelDialogViewModel? ViewModel { get; set; }

        public EditChannelDialogRoutedEventArgs() : base()
        {
        }

        public EditChannelDialogRoutedEventArgs(EditChannelDialogViewModel viewModel) : base()
        {
            ViewModel = viewModel;
        }
    }
}
