using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DataLayer.Models;

namespace CrybbBot.ViewModels;

public class MessagesFilterDialogViewModel : ModelBase
{
    public List<BundleFlag> Bundles { get; } = new();
    public List<ChannelFlag> Channels { get; } = new();

    private MessagesFilter _filter = MessagesFilter.Default;

    public MessagesFilter Filter
    {
        get => _filter;
        set
        {
            _filter = value;
            RaisePropertyChanged();
            RaisePropertyChanged("SelectAll");
        }
    }

    public bool Ascending
    {
        get => Filter.Ascending;
        set
        {
            _filter.Ascending = value;
            RaisePropertyChanged();
            RaisePropertyChanged("Descending");
        }
    }

    public bool Descending
    {
        get => !Filter.Ascending;
        set
        {
            _filter.Ascending = !value;
            RaisePropertyChanged();
            RaisePropertyChanged("Ascending");
        }
    }

    public bool SelectAll
    {
        get => Filter.SelectAll;
        set
        {
            _filter.Scheduled = null;
            _filter.Sent = null;
            _filter.Errors = null;
            _filter.Attachments = null;
            _filter.ContainingText = false;
            _filter.Text = null;
            RaisePropertyChanged();
            RaisePropertyChanged("Scheduled");
            RaisePropertyChanged("NotScheduled");
            RaisePropertyChanged("Sent");
            RaisePropertyChanged("NotSent");
            RaisePropertyChanged("WithErrors");
            RaisePropertyChanged("WithoutErrors");
            RaisePropertyChanged("WithAttachments");
            RaisePropertyChanged("WithoutAttachments");
            RaisePropertyChanged("ContainingText");
            RaisePropertyChanged("Text");
        }
    }

    public bool Scheduled
    {
        get => Filter.Scheduled.HasValue && Filter.Scheduled.Value;
        set
        {
            if (value)
            {
                _filter.Scheduled = true;
                RaisePropertyChanged();
                RaisePropertyChanged("NotScheduled");
            }
            else if (!NotScheduled)
            {
                _filter.Scheduled = null;
                RaisePropertyChanged();
            }
            RaisePropertyChanged("SelectAll");
        }
    }

    public bool NotScheduled
    {
        get => Filter.Scheduled.HasValue && !Filter.Scheduled.Value;
        set
        {
            if (value)
            {
                _filter.Scheduled = false;
                RaisePropertyChanged();
                RaisePropertyChanged("Scheduled");
            }
            else if (!Scheduled)
            {
                _filter.Scheduled = null;
                RaisePropertyChanged();
            }
            RaisePropertyChanged("SelectAll");
        }
    }

    public bool Sent
    {
        get => Filter.Sent.HasValue && Filter.Sent.Value;
        set
        {
            if (value)
            {
                _filter.Sent = true;
                RaisePropertyChanged();
                RaisePropertyChanged("NotSent");
            }
            else
            {
                if (!NotSent)
                {
                    _filter.Sent = null;
                    RaisePropertyChanged();
                }
                WithErrors = false;
                WithoutErrors = false;
            }
            RaisePropertyChanged("SelectAll");
        }
    }

    public bool NotSent
    {
        get => Filter.Sent.HasValue && !Filter.Sent.Value;
        set
        {
            if (value)
            {
                _filter.Sent = false;
                RaisePropertyChanged();
                RaisePropertyChanged("Sent");
                WithErrors = false;
                WithoutErrors = false;
            }
            else if (!Sent)
            {
                _filter.Sent = null;
                RaisePropertyChanged();
            }
            RaisePropertyChanged("SelectAll");
        }
    }

    public bool WithErrors
    {
        get => Filter.Errors.HasValue && Filter.Errors.Value;
        set
        {
            if (value)
            {
                _filter.Errors = true;
                RaisePropertyChanged();
                RaisePropertyChanged("WithoutErrors");
                Sent = true;
            }
            else if (!WithoutErrors)
            {
                _filter.Errors = null;
                RaisePropertyChanged();
            }
            RaisePropertyChanged("SelectAll");
        }
    }

    public bool WithoutErrors
    {
        get => Filter.Errors.HasValue && !Filter.Errors.Value;
        set
        {
            if (value)
            {
                _filter.Errors = false;
                RaisePropertyChanged();
                RaisePropertyChanged("WithErrors");
                Sent = true;
            }
            else if (!WithErrors)
            {
                _filter.Errors = null;
                RaisePropertyChanged();
            }
            RaisePropertyChanged("SelectAll");
        }
    }

    public bool WithAttachments
    {
        get => Filter.Attachments.HasValue && Filter.Attachments.Value;
        set
        {
            if (value)
            {
                _filter.Attachments = true;
                RaisePropertyChanged();
                RaisePropertyChanged("WithoutAttachments");
            }
            else if (!WithoutAttachments)
            {
                _filter.Attachments = null;
                RaisePropertyChanged();
            }
            RaisePropertyChanged("SelectAll");
        }
    }

    public bool WithoutAttachments
    {
        get => Filter.Attachments.HasValue && !Filter.Attachments.Value;
        set
        {
            if (value)
            {
                _filter.Attachments = false;
                RaisePropertyChanged();
                RaisePropertyChanged("WithAttachments");
            }
            else if (!WithAttachments)
            {
                _filter.Attachments = null;
                RaisePropertyChanged();
            }
            RaisePropertyChanged("SelectAll");
        }
    }

    public bool AttachmentsAll
    {
        get => (Filter.AttType & MessagesFilter.AttachmentsType.All) == MessagesFilter.AttachmentsType.All;
        set
        {
            if (value)
            {
                Filter.AttType |= MessagesFilter.AttachmentsType.All;
            }
            else
            {
                _filter.AttType &= ~MessagesFilter.AttachmentsType.All;
            }
            RaisePropertyChanged();
            RaisePropertyChanged("AttachmentsImages");
            RaisePropertyChanged("AttachmentsFiles");
            RaisePropertyChanged("AttachmentsFileReferences");
        }
    }

    public bool AttachmentsImages
    {
        get => (Filter.AttType & MessagesFilter.AttachmentsType.Image) == MessagesFilter.AttachmentsType.Image;
        set
        {
            if (value)
            {
                Filter.AttType |= MessagesFilter.AttachmentsType.Image;
                RaisePropertyChanged();
            }
            else
            {
                _filter.AttType &= ~MessagesFilter.AttachmentsType.Image;
                RaisePropertyChanged();
            }
            RaisePropertyChanged("AttachmentsAll");
        }
    }

    public bool AttachmentsFiles
    {
        get => (Filter.AttType & MessagesFilter.AttachmentsType.File) == MessagesFilter.AttachmentsType.File;
        set
        {
            if (value)
            {
                Filter.AttType |= MessagesFilter.AttachmentsType.File;
                RaisePropertyChanged();
            }
            else
            {
                _filter.AttType &= ~MessagesFilter.AttachmentsType.File;
                RaisePropertyChanged();
            }
            RaisePropertyChanged("AttachmentsAll");
        }
    }

    public bool AttachmentsFileReferences
    {
        get => (Filter.AttType & MessagesFilter.AttachmentsType.FileReference) == MessagesFilter.AttachmentsType.FileReference;
        set
        {
            if (value)
            {
                Filter.AttType |= MessagesFilter.AttachmentsType.FileReference;
                RaisePropertyChanged();
            }
            else
            {
                _filter.AttType &= ~MessagesFilter.AttachmentsType.FileReference;
                RaisePropertyChanged();
            }
            RaisePropertyChanged("AttachmentsAll");
        }
    }

    public bool ContainingText
    {
        get => Filter.ContainingText;
        set
        {
            _filter.ContainingText = value;
            RaisePropertyChanged();
            RaisePropertyChanged("SelectAll");
        }
    }

    public string Text
    {
        get => Filter.Text ?? string.Empty;
        set
        {
            Filter.Text = string.IsNullOrWhiteSpace(value) ? null : value;
            RaisePropertyChanged();
            RaisePropertyChanged("SelectAll");
        }
    }

    private bool _scheduledDtFrom;
    public bool ScheduledDtFrom
    {
        get => _scheduledDtFrom;
        set
        {
            _scheduledDtFrom = value;
            RaisePropertyChanged();
        }
    }

    private DateTime? _scheduledExact;
    public DateTime? ScheduledExact
    {
        get => _scheduledExact;
        set
        {
            _scheduledExact = value;
            RaisePropertyChanged();
            ScheduledDtFrom = CreatedFrom.HasValue || CreatedTo.HasValue || ScheduledExact.HasValue;
        }
    }

    private DateTime? _scheduledFrom;
    public DateTime? ScheduledFrom
    {
        get => _scheduledFrom;
        set
        {
            if (_scheduledTo.HasValue && _scheduledTo < value)
            {
                _scheduledFrom = _scheduledTo;
                _scheduledTo = value;
                RaisePropertyChanged("ScheduledTo");
            }
            else
            {
                _scheduledFrom = value;
            }
            RaisePropertyChanged();
            ScheduledDtFrom = CreatedFrom.HasValue || CreatedTo.HasValue || ScheduledExact.HasValue;
        }
    }

    private DateTime? _scheduledTo;
    public DateTime? ScheduledTo
    {
        get => _scheduledTo;
        set
        {
            if (_scheduledFrom.HasValue && _scheduledFrom > value)
            {
                _scheduledTo = _scheduledFrom;
                _scheduledFrom = value;
                RaisePropertyChanged("ScheduledFrom");
            }
            else
            {
                _scheduledTo = value;
            }
            RaisePropertyChanged();
            ScheduledDtFrom = CreatedFrom.HasValue || CreatedTo.HasValue || ScheduledExact.HasValue;
        }
    }

    private bool _createdDtFrom;
    public bool CreatedDtFrom
    {
        get => _createdDtFrom;
        set
        {
            _createdDtFrom = value;
            RaisePropertyChanged();
        }
    }

    private DateTime? _createdExact;
    public DateTime? CreatedExact
    {
        get => _createdExact;
        set
        {
            _createdExact = value;
            RaisePropertyChanged();
            CreatedDtFrom = CreatedFrom.HasValue || CreatedTo.HasValue || CreatedExact.HasValue;
        }
    }

    private DateTime? _createdFrom;
    public DateTime? CreatedFrom
    {
        get => _createdFrom;
        set
        {
            if (_createdTo.HasValue && _createdTo < value)
            {
                _createdFrom = _createdTo;
                _createdTo = value;
                RaisePropertyChanged("CreatedTo");
            }
            else
            {
                _createdFrom = value;
            }
            RaisePropertyChanged();
            CreatedDtFrom = CreatedFrom.HasValue || CreatedTo.HasValue || CreatedExact.HasValue;
        }
    }

    private DateTime? _createdTo;
    public DateTime? CreatedTo
    {
        get => _createdTo;
        set
        {
            if (_createdFrom.HasValue && _createdFrom > value)
            {
                _createdTo = _createdFrom;
                _createdFrom = value;
                RaisePropertyChanged("CreatedFrom");
            }
            else
            {
                _createdTo = value;
            }
            RaisePropertyChanged();
            CreatedDtFrom = CreatedFrom.HasValue || CreatedTo.HasValue || CreatedExact.HasValue;
        }
    }

    public bool SearchByChannels
    {
        get => Filter.SearchByChannels;
        set
        {
            Filter.SearchByChannels = value;
            RaisePropertyChanged();
            RaisePropertyChanged("AnySelectedChannel");
        }
    }

    public bool AnySelectedChannel => SearchByChannels && (Bundles.Any(p => p.Flag) || Channels.Any(p => p.Flag));

    private int _transitionerIndex = 0;
    public int TransitionerIndex
    {
        get => _transitionerIndex;
        set
        {
            _transitionerIndex = value;
            RaisePropertyChanged();
            RaisePropertyChanged("SwitchToMessagesVisibility");
            RaisePropertyChanged("SwitchToChannelsVisibility");
        }
    }

    public Visibility SwitchToMessagesVisibility => TransitionerIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SwitchToChannelsVisibility => TransitionerIndex == 1 ? Visibility.Visible : Visibility.Collapsed;

    public ICommand SwitchToMessagesCommand { get; private set; }
    public ICommand SwitchToChannelsCommand { get; private set; }
    public ICommand RemoveBundleSelectionCommand { get; private set; }
    public ICommand RemoveChannelSelectionCommand { get; private set; }

    public MessagesFilterDialogViewModel()
    {
        InitCommands();
        LoadChannels();
    }

    public MessagesFilterDialogViewModel(MessagesFilter? filter)
    {
        InitCommands();

        if (filter == null)
        {
            _filter = MessagesFilter.Default;
            LoadChannels();
            return;
        }

        _filter = new MessagesFilter
        {
            Ascending = filter.Ascending,
            CreatedFrom = filter.CreatedFrom,
            CreatedTo = filter.CreatedTo,
            ScheduledFrom = filter.ScheduledFrom,
            ScheduledTo = filter.ScheduledTo,
            Scheduled = filter.Scheduled,
            Sent = filter.Sent,
            Errors = filter.Errors,
            Attachments = filter.Attachments,
            AttType = filter.AttType,
            ContainingText = filter.ContainingText,
            Text = filter.Text,
            SearchByChannels = filter.SearchByChannels
        };

        if (filter.Bundles != null && filter.Bundles.Any())
        {
            _filter.Bundles = new List<Guid>();
            _filter.Bundles.AddRange(filter.Bundles);
        }

        if (filter.Channels != null && filter.Channels.Any())
        {
            _filter.Channels = new List<Guid>();
            _filter.Channels.AddRange(filter.Channels);
        }

        LoadChannels();
    }

    private void InitCommands()
    {
        SwitchToMessagesCommand = new RelayCommand(SwitchToMessages);
        SwitchToChannelsCommand = new RelayCommand(SwitchToChannels);
        RemoveBundleSelectionCommand = new RelayCommand(RemoveBundleSelection);
        RemoveChannelSelectionCommand = new RelayCommand(RemoveChannelSelection);
    }

    private void LoadChannels()
    {
        var bundles = ChannelBundleViewModel.GlobalCollection
            ?.Where(p => p.DbBundle != null)
            .Select(p => new BundleFlag
            {
                Flag = _filter.Bundles == null || !_filter.Bundles.Any()
                    ? false
                    : _filter.Bundles.Any(q => q.Equals(p.DbBundle!.ID)),
                ID = p.DbBundle!.ID,
                Alias = p.DbBundle.Alias!
            })
            .DistinctBy(p => p!.ID)
            .ToList();
        
        var channels = ChannelBundleViewModel.GlobalCollection
            ?.SelectMany(p => p.Channels)
            .Where(p => p.DbChannel != null)
            .DistinctBy(p => p.DbChannel!.ID)
            .Select(p => new ChannelFlag
            {
                Flag = _filter.Channels == null || !_filter.Channels.Any()
                    ? false
                    : _filter.Channels.Any(q => q.Equals(p.DbChannel!.ID)),
                ID = p.DbChannel!.ID,
                Alias = p.DbChannel.Alias!
            })
            .ToList();
        
        Bundles.Clear();
        if (bundles != null)
        {
            Bundles.AddRange(bundles!);
        }

        Channels.Clear();
        if (channels != null)
        {
            Channels.AddRange(channels);
        }

        RaisePropertyChanged("AnySelectedChannel");
    }

    private void SwitchToMessages()
    {
        TransitionerIndex = 0;
        RaisePropertyChanged("AnySelectedChannel");
    }

    private void SwitchToChannels()
    {
        TransitionerIndex = 1;
    }

    private void RemoveBundleSelection()
    {
        Bundles.ForEach(p => p.Flag = false);
        UpdateBundles();
    }

    private void RemoveChannelSelection()
    {
        Channels.ForEach(p => p.Flag = false);
        UpdateChannels();
    }

    public void UpdateBundles()
    {
        Filter.Bundles?.Clear();

        if (Bundles.Any())
        {
            if (Filter.Bundles == null)
            {
                Filter.Bundles = new List<Guid>();
            }
            Filter.Bundles.AddRange(Bundles.Where(p => p.Flag).Select(p => p.ID));
        }
        else
        {
            Filter.Bundles = null;
        }
    }

    public void UpdateChannels()
    {
        Filter.Channels?.Clear();

        if (Channels.Any())
        {
            if (Filter.Channels == null)
            {
                Filter.Channels = new List<Guid>();
            }
            Filter.Channels.AddRange(Channels.Where(p => p.Flag).Select(p => p.ID));
        }
        else
        {
            Filter.Channels = null;
        }
    }
}
