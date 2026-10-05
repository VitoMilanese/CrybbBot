using MaterialDesignThemes.Wpf;
using System.Threading.Tasks;

namespace CrybbBot.Services;

public class DialogService
{
    private readonly string _id;
    public DialogService(string id) => _id = id;
    public Task<object?> ShowAsync(object content) =>
        DialogHost.Show(content, _id);
}