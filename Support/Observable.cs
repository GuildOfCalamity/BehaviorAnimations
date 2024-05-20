using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BehaviorAnimations;

/// <summary>
/// This is here in the event you don't want to use the CommunityToolkit.Mvvm.ComponentModel library.
/// <example>From inside your ViewModel:<code>
/// public bool IsBusy
/// {
///     get => _isBusy;
///     set {
///         if (value != _isBusy) {
///            _isBusy = value;
///            OnPropertyChanged(nameof(IsBusy));
///         }
///     }
/// }
/// 
/// public bool IsEnabled
/// {
///     get => _isEnabled;
///     set { 
///         Set(ref _isEnabled, value); 
///     }
/// }
///
/// </code></example>
/// </summary>
public class Observable : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void Set<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value))
            return;

        storage = value;
        OnPropertyChanged(propertyName);
    }

    protected void OnPropertyChanged(string? propertyName)
    {
        if (!string.IsNullOrEmpty(propertyName))
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
