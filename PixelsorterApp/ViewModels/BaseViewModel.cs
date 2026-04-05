using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PixelsorterApp.ViewModels;

/// <summary>
/// Provides basic property change notification helpers for view models.
/// </summary>
public abstract class BaseViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sets a backing field and raises <see cref="PropertyChanged"/> when the value changes.
    /// </summary>
    /// <typeparam name="T">The property value type.</typeparam>
    /// <param name="storage">The backing field reference.</param>
    /// <param name="value">The new value.</param>
    /// <param name="propertyName">The property name. Populated automatically by the compiler.</param>
    /// <returns><see langword="true"/> if the value changed; otherwise, <see langword="false"/>.</returns>
    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for a property.
    /// </summary>
    /// <param name="propertyName">The property name. Populated automatically by the compiler.</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
