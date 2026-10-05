using System.ComponentModel;
using System.Runtime.CompilerServices;
using CrybbBot.ViewModels;

namespace CrybbBot.Models;

public sealed class ChannelItem : INotifyPropertyChanged
{
    public bool All { get; }
    public string Name { get; }
    public ChannelBundleViewModel? Bundle { get; }
    public ChannelViewModel? Channel { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public ChannelItem(string name, bool all = false)
    {
        Name = name;
        All = all;
    }

    public ChannelItem(ChannelViewModel channel, bool all = false)
    {
        Channel = channel;
        Name = channel.Alias ?? channel.Id;
        All = all;
    }

    public ChannelItem(ChannelBundleViewModel bundle, bool all = false)
    {
        Bundle = bundle;
        Name = bundle.Text;
        All = all;
    }

    public override string ToString() => Name;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
