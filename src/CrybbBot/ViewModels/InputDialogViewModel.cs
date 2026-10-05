namespace CrybbBot.ViewModels;

public class InputDialogViewModel : ModelBase
{
    public string? Title { get; }
    public string? Message { get; }

    private string _value;
    public string Value
    {
        get => _value;
        set
        {
            _value = value;
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

    public InputDialogViewModel() => (MaxLength, Title, Message) = (0, null, null);
    public InputDialogViewModel(string t) => (MaxLength, Title, Message) = (0, t, null);
    public InputDialogViewModel(string? t, string? m) => (MaxLength, Title, Message) = (0, t, m);
    public InputDialogViewModel(int maxLength) => (MaxLength, Title, Message) = (maxLength, null, null);
    public InputDialogViewModel(int maxLength, string t) => (MaxLength, Title, Message) = (maxLength, t, null);
    public InputDialogViewModel(int maxLength, string? t, string? m) => (MaxLength, Title, Message) = (maxLength, t, m);
}
