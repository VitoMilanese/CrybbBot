using System.Security.Policy;

namespace CrybbBot.ViewModels;

public class FileReferenceDialogViewModel : ModelBase
{
    private string _title;
    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            RaisePropertyChanged();
        }
    }

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

    public FileReferenceDialogViewModel() => MaxLength = 0;
    
    public FileReferenceDialogViewModel(string title, string url)
    {
        (MaxLength, Title, Url) = (0, title, url);
    }

    public FileReferenceDialogViewModel(int titleMaxLength) => MaxLength = titleMaxLength;
    
    public FileReferenceDialogViewModel(int titleMaxLength, string title, string url)
    {
        (MaxLength, Title, Url) = (titleMaxLength, title, url);
    }
}
