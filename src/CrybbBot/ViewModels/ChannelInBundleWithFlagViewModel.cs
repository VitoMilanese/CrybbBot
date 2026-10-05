namespace CrybbBot.ViewModels;

public sealed class ChannelInBundleWithFlagViewModel : ModelBase
{
    public bool WasPresent { get; set; }

    private bool _isPresent;
    public bool IsPresent
    {
        get => _isPresent;
        set
        {
            _isPresent = value;
            RaisePropertyChanged();
        }
    }

    public bool Changed => IsPresent != WasPresent;

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            RaisePropertyChanged();
        }
    }

    public string _bundleName = string.Empty;
    public string BundleName
    {
        get => _bundleName;
        set
        {
            _bundleName = value;
            RaisePropertyChanged();
        }
    }
}
