using System;
using System.Diagnostics;
using System.Windows.Input;

namespace WPFTimer;

interface IRelayCommand : ICommand {
    void NotifyCanExecuteChanged();
}

class RelayCommand : IRelayCommand {
    readonly Action _execute;
    readonly Func<bool>? _canExecute;

    public event EventHandler? CanExecuteChanged {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public void NotifyCanExecuteChanged() {
        CommandManager.InvalidateRequerySuggested();
    }

    public bool CanExecute(object? parameter) {
        return _canExecute is null ? true : _canExecute();
    }

    public void Execute(object? parameter) {
        _execute();
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = default) {
        _execute = execute; _canExecute = canExecute;
    }
}

class RelayCommand<T> : IRelayCommand {
    readonly Action<T> _execute;
    readonly Func<T, bool>? _canExecute;

    public event EventHandler? CanExecuteChanged {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public void NotifyCanExecuteChanged() {
        CommandManager.InvalidateRequerySuggested();
    }

    public bool CanExecute(object? parameter) {
        // Debug.WriteLine($"RelayCommand.CanExecute({parameter})");
        return (parameter is T param) && (_canExecute is null ? true : _canExecute(param));
    }

    public void Execute(object? parameter) {
        if (parameter is not null && parameter is T value) {
            _execute(value);
        }
        else throw new ArgumentException(
            "Argument must not be null or wrong type", nameof(parameter));
    }

    public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = default) {
        _execute = execute; _canExecute = canExecute;
    }
}