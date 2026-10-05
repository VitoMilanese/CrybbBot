using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CrybbBot.ViewModels;

public class ModelBase : ValidationBase, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public void RaisePropertyChanged([CallerMemberName] string propertyName = null)
    {
        AssertPropertyExists(propertyName);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected void NotifyPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// Warns the developer if this object does not have a public property with the specified name.
    /// This method does not exist in a Release build.
    /// </summary>
    [Conditional("DEBUG")]
    [DebuggerStepThrough]
    public virtual void AssertPropertyExists(string propertyName)
    {
        // Verify that the property name matches a real, public, instance property on this object.
        var properties = TypeDescriptor.GetProperties(this);
        if (properties[propertyName] == null)
            Debug.Fail("Invalid property name: {propertyName}");
    }
}
