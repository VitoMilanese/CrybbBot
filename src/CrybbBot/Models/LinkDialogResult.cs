using CrybbBot.ViewModels;

namespace CrybbBot.Models;

public sealed class LinkDialogResult : ModelBase
{
    public bool Yes { get; init; }

    private string? _url;
    public string? Url
    {
        get => _url;
        set
        {
            _url = value;
            RaisePropertyChanged();
        }
    }
}
