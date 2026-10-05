namespace CrybbBot.ViewModels;

public class AddChannelDialogViewModel : ModelBase
{
    public string? Title { get; }

    private string _channelId;
    public string ChannelId
    {
        get => _channelId;
        set
        {
            _channelId = value;
            RaisePropertyChanged();
        }
    }

    private string _alias;
    public string Alias
    {
        get => _alias;
        set
        {
            _alias = value;
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

    private bool _focusTextBox;
    public bool FocusTextBox
    {
        get => _focusTextBox;
        set
        {
            _focusTextBox = value;
            RaisePropertyChanged();
        }
    }

    public AddChannelDialogViewModel() => (MaxLength, Title) = (0, null);
    public AddChannelDialogViewModel(string? t) => (MaxLength, Title) = (0, t);
    public AddChannelDialogViewModel(int maxLength) => (MaxLength, Title) = (maxLength, null);
    public AddChannelDialogViewModel(int maxLength, string? t) => (MaxLength, Title) = (maxLength, t);
}
