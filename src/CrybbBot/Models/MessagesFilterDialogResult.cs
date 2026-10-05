using CrybbBot.ViewModels;
using DataLayer.Models;

namespace CrybbBot.Models;

public sealed class MessagesFilterDialogResult : ModelBase
{
    public bool Yes { get; init; }

    private MessagesFilter? _filter;
    public MessagesFilter? Filter
    {
        get => _filter;
        set
        {
            _filter = value;
            RaisePropertyChanged();
        }
    }
}
