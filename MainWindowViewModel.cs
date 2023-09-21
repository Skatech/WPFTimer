using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Input;
using System.Diagnostics.Metrics;
using System.Windows.Automation.Peers;
using System.Windows;
using System.Collections;
using System.Diagnostics;
using Microsoft.Win32;
using System.Threading;
using System.Media;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks.Dataflow;
using System.Collections.Concurrent;

namespace WPFTimer;

class MainViewModel : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<bool>? RingtoneRequested;
    public ObservableCollection<TimerView> Tasks { get; } = new();
    public IEnumerable<StartButtonViewModel> Intervals { get; }

    public MainViewModel() {
        var startTimerCommand = new RelayCommand<int>(StartTimer);
        Intervals = new[] { 5, 10, 15, 20, 25, 30, 40, 50, 60, 90, 120, 180 }
            .Select(it => new StartButtonViewModel(it, startTimerCommand)).ToArray();
    }

    public void StartTimer(int interval) {
        AddTask(new TimerView(interval, OnRingtoneRequested));
    }

    void AddTask(TimerView view) {
        Tasks.Add(view);
        view.Worker.ContinueWith(async _ => await Task.Delay(100))
            .Unwrap().ContinueWith(_ => Tasks.Remove(view),
                TaskScheduler.FromCurrentSynchronizationContext());
    }

    void OnRingtoneRequested(bool enabled) {
        RingtoneRequested?.Invoke(enabled);
    }

    void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName]string? name = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

class StartButtonViewModel {
    public string Title { get; private set; }
    public int MinutesDelay { get; private set; }
    public ICommand Command { get; private set; }

    public StartButtonViewModel(int minutesDelay, ICommand command) {
        Title = (MinutesDelay = minutesDelay) + " min";
        Command = command;
    }
}

class TimerView : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;
    public ICommand StopCommand { get; }
    public Task Worker { get; }
    public TimeSpan Timeout { get; private set; }
    public Brush ForegroundBrush { get; private set; } = Brushes.Black;

    CancellationTokenSource _cts = new();
    Action<bool> _ringtoneRequest;

    public TimerView(int minutesDelay, Action<bool> ringtoneRequest) {
        _ringtoneRequest = ringtoneRequest;
        StopCommand = new RelayCommand(Stop);
        Timeout = TimeSpan.FromMinutes(minutesDelay);
        Worker = Run(_cts.Token);
    }

    async Task Run(CancellationToken cancellationToken) {
        TimeSpan delay = Timeout;
        Stopwatch sw = Stopwatch.StartNew();
        while (delay > sw.Elapsed) {
            Timeout = delay - sw.Elapsed;
            OnPropertyChanged(nameof(Timeout));
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        }

        _ringtoneRequest(true);
        using var finalizer = new FinalAction(() => _ringtoneRequest(false));
        while (true) {
            ForegroundBrush = (sw.Elapsed.Seconds % 2) > 0 ? Brushes.Red : Brushes.Black;            
            OnPropertyChanged(nameof(ForegroundBrush));
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        }
    }

    void Stop() {
        _cts.Cancel();
    }

    void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName]string? name = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    struct FinalAction :IDisposable {
        readonly Action _action;

        public FinalAction(Action action) {
            _action = action;
        }

        public void Dispose() {
            _action();
        }
    }
}
