namespace CrybbBot.ViewModels;

public class YesNoDialogViewModel : ModelBase
{
    public string Title { get; }
    public string Message { get; }

    public YesNoDialogViewModel(string t, string m) => (Title, Message) = (t, m);
}
