using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using CrybbBot.Models;

namespace CrybbBot.ViewModels;

public sealed class ChannelMultiSelectComboBoxViewModel : INotifyPropertyChanged
{
    public ObservableCollection<ChannelItem> Items { get; } = new();
    public ObservableCollection<ChannelItem> SelectedItems { get; } = new();

    private bool _isEditMode = true;
    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            if (_isEditMode == value) return;
            _isEditMode = value;
            OnPropertyChanged();
        }
    }

    private bool _isDropDownOpen;
    public bool IsDropDownOpen
    {
        get => _isDropDownOpen;
        set
        {
            if (_isDropDownOpen == value) return;
            _isDropDownOpen = value;
            OnPropertyChanged();
        }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value;
            OnPropertyChanged();
            ChannelsView.Refresh();
        }
    }

    private bool _isAllSelected;
    public bool IsAllSelected
    {
        get => _isAllSelected;
        set
        {
            if (_isAllSelected == value) return;
            _isAllSelected = value;
            OnPropertyChanged();
            ApplyAllSelectionMode();
            OnPropertyChanged(nameof(IsListEnabled));
        }
    }
    public bool IsListEnabled => !IsAllSelected;

    public ICollectionView ChannelsView { get; }

    public ICommand ToggleItemCommand { get; }
    public ICommand RemoveSelectedCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand SelectAllCommand { get; }

    public ICommand CreateNewChannelCommand { get; }

    public ChannelMultiSelectComboBoxViewModel()
    {
        ToggleItemCommand = new RelayCommand<ChannelItem>(ToggleItem);
        RemoveSelectedCommand = new RelayCommand<ChannelItem>(RemoveSelected);
        ClearAllCommand = new RelayCommand(ClearAll);
        SelectAllCommand = new RelayCommand(SelectAll);
        CreateNewChannelCommand = new RelayCommand(() => { });

        SelectedItems.CollectionChanged += SelectedItemsChanged;

        ChannelsView = CollectionViewSource.GetDefaultView(Items);
        ChannelsView.Filter = FilterChannels;
    }

    private void ToggleItem(ChannelItem item)
    {
        // If All-mode is on, block other picks
        if (IsAllSelected && !item.All)
            return;

        // Handle "All" explicitly and exit (avoids re-entrancy)
        if (item.All)
        {
            IsAllSelected = !IsAllSelected;
            return;
        }

        // Normal toggle
        if (item.IsSelected)
        {
            item.IsSelected = false;
            SelectedItems.Remove(item);
        }
        else
        {
            item.IsSelected = true;
            if (!SelectedItems.Contains(item))
                SelectedItems.Add(item);
        }
    }

    private void RemoveSelected(ChannelItem item)
    {
        if (item.All)
        {
            IsAllSelected = false;
            return;
        }

        item.IsSelected = false;
        SelectedItems.Remove(item);
    }

    private void ClearAll()
    {
        IsAllSelected = false;

        foreach (var s in SelectedItems.ToList())
            s.IsSelected = false;

        SelectedItems.Clear();
    }

    private void SelectAll()
    {
        IsAllSelected = !IsAllSelected;
        ApplyAllSelectionMode();
    }

    private void SelectedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (ChannelItem i in e.NewItems) i.IsSelected = true;

        if (e.OldItems != null)
            foreach (ChannelItem i in e.OldItems) i.IsSelected = false;
    }

    private bool FilterChannels(object obj)
    {
        if (obj is not ChannelItem ch)
            return false;

        if (ch.All)
            return true;

        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        return ch.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyAllSelectionMode()
    {
        var all = Items.FirstOrDefault(x => x.All);
        if (all == null) return;

        if (IsAllSelected)
        {
            // Clear other selections
            foreach (var s in SelectedItems.ToList())
                s.IsSelected = false;

            SelectedItems.Clear();
            
            SearchText = string.Empty;

            // Select only "All"
            all.IsSelected = true;
            if (!SelectedItems.Contains(all))
                SelectedItems.Add(all);
        }
        else
        {
            // Unselect "All"
            all.IsSelected = false;
            SelectedItems.Remove(all);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
