using System;

namespace CrybbBot.ViewModels;

public class RenameDialogViewModel : ModelBase
{
    public string Title { get; }

    private string _oldValue;
    public string OldValue
    {
        get => _oldValue;
        set
        {
            _oldValue = value;
            RaisePropertyChanged();
        }
    }

    private string? _newValue;
    public string? NewValue
    {
        get => _newValue;
        set
        {
            _newValue = value;
            RaisePropertyChanged();
        }
    }

    private string _caption1 = "Старе значення:";
    public string Caption1
    {
        get => _caption1;
        private set
        {
            _caption1 = value;
            RaisePropertyChanged();
        }
    }

    private string _caption2 = "Нове значення:";
    public string Caption2
    {
        get => _caption2;
        private set
        {
            _caption2 = value;
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

    private bool _focusNewValue;
    public bool FocusNewValue
    {
        get => _focusNewValue;
        set
        {
            _focusNewValue = value;
            RaisePropertyChanged();
        }
    }

    public RenameDialogViewModel(string title, string oldValue, Tuple<string, string>? captions = null)
    {
        (MaxLength, Title, OldValue, NewValue) = (0, title, oldValue, string.Empty);
        if (captions != null)
        {
            Caption1 = captions.Item1;
            Caption2 = captions.Item2;
        }
    }

    public RenameDialogViewModel(int maxLength, string title, string oldValue, Tuple<string, string>? captions = null)
    {
        (MaxLength, Title, OldValue) = (maxLength, title, oldValue);
        if (captions != null)
        {
            Caption1 = captions.Item1;
            Caption2 = captions.Item2;
        }
    }
}
