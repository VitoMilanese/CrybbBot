namespace CrybbBot.ViewModels;

public class LinkDialogViewModel : ModelBase
{
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

    private bool _focusTitle;
    public bool FocusTitle
    {
        get => _focusTitle;
        set
        {
            _focusTitle = value;
            RaisePropertyChanged();
        }
    }

    private bool _isPreview;
    public bool IsPreview
    {
        get => _isPreview;
        set
        {
            _isPreview = value;
            RaisePropertyChanged();
        }
    }

    public LinkDialogViewModel()
    {
    }
    
    public LinkDialogViewModel(string url)
    {
        Url = url;
    }
    
    public LinkDialogViewModel(string url, bool preview)
    {
        Url = url;
        IsPreview = preview;
    }
}
