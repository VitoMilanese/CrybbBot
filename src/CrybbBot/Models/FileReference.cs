using CrybbBot.ViewModels;

namespace CrybbBot.Models;

public sealed class FileReference : ModelBase
{
    private string? _title;
    public string? Title
    {
        get => _title;
        set
        {
            _title = value;
            RaisePropertyChanged();
            RaisePropertyChanged("TitleOrUrl");
        }
    }

    public string? TitleOrUrl => string.IsNullOrWhiteSpace(_title) ? Url : _title;

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
