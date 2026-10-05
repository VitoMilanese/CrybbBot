namespace CrybbBot.ViewModels;

public class MessageDialogViewModel : ModelBase
{
    public string Title { get; }
    public string Message { get; }
    public bool IsReadOnly { get; }

    public MessageDialogViewModel(string t, string m) => (Title, Message) = (t, m);
    public MessageDialogViewModel(string t, string m, bool isReadonly) => (Title, Message, IsReadOnly) = (t, m, isReadonly);
}
