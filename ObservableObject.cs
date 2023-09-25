using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace WPFTimer;

class ObservableObject : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName]string? name = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    protected bool SetProperty<T>(
            [System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(newValue))] ref T field, T newValue,
            [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) {
        if (EqualityComparer<T>.Default.Equals(field, newValue)) {
            return false;
        }
        field = newValue;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected bool SetProperty<T>(T oldValue, T newValue, Action<T> callback,
            [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) {
        ArgumentNullException.ThrowIfNull(callback);
        if (EqualityComparer<T>.Default.Equals(oldValue, newValue)) {
            return false;
        }
        callback(newValue);
        OnPropertyChanged(propertyName);
        return true;
    }
}