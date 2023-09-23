using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Input;
using System.Diagnostics;
using System.Threading;

namespace WPFTimer;

class MainViewModel : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<TimerView> Tasks { get; } = new();
    public IEnumerable<StartButtonViewModel> Intervals { get; }
    public double NearestProgress { get; private set; }
    public bool RingtoneEnabled { get; private set; }

    public MainViewModel() {
        var startTimerCommand = new RelayCommand<int>(StartTimer);
        Intervals = new[] { 5, 10, 15, 20, 25, 30, 40, 50, 60, 90, 120, 180 }
            .Select(it => new StartButtonViewModel(it, startTimerCommand)).ToArray();
    }

    public void StartTimer(int interval) {
        var timer = new TimerView(interval);
        timer.PropertyChanged += OnTimerViewPropertyChanged;
        lock (Tasks) Tasks.Add(timer);
        timer.Start();
    }

    void OnTimerViewPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (sender is TimerView timer) {
            if (nameof(TimerView.Progress).Equals(e.PropertyName)) {
                if (timer.Progress > NearestProgress) {
                    NearestProgress = timer.Progress;
                    OnPropertyChanged(nameof(NearestProgress));

                    if (RingtoneEnabled != (NearestProgress == 1.0)) {
                        RingtoneEnabled = (NearestProgress == 1.0); 
                        OnPropertyChanged(nameof(RingtoneEnabled));
                    }
                }
            }
            else if (e.PropertyName is null) { // timer stopped
                timer.PropertyChanged -= OnTimerViewPropertyChanged;                
                lock(Tasks) {
                    Tasks.Remove(timer);
                    NearestProgress = Tasks.Count > 0 ? Tasks.Max(e => e.Progress) : 0;
                }
                OnPropertyChanged(nameof(NearestProgress));
                if (RingtoneEnabled != (NearestProgress == 1.0)) {
                    RingtoneEnabled = (NearestProgress == 1.0); 
                    OnPropertyChanged(nameof(RingtoneEnabled));
                }
            }
        }
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
    public Task? Worker { get; private set; }
    public TimeSpan Timeout { get; private set; }
    public Brush ForegroundBrush { get; private set; } = Brushes.Black;
    public double Progress { get; private set; }

    CancellationTokenSource _cts = new();
    
    public TimerView(int minutesDelay) {
        StopCommand = new RelayCommand(Stop);
        Timeout = TimeSpan.FromMinutes(minutesDelay);
    }

    public void Start() {
        if (Worker == null) {
            Worker = Run(_cts.Token);
            Worker.ContinueWith(_ =>  OnPropertyChanged(null), // signal timer stopped
                TaskScheduler.FromCurrentSynchronizationContext());
        }
        else throw new InvalidOperationException("TimerView already started.");
    }

    async Task Run(CancellationToken cancellationToken) {
        TimeSpan delay = Timeout;
        Stopwatch sw = Stopwatch.StartNew();
        while (delay > sw.Elapsed) {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            Timeout = delay - sw.Elapsed;
            OnPropertyChanged(nameof(Timeout));            
            Progress = Math.Min(1.0, sw.Elapsed / delay);
            OnPropertyChanged(nameof(Progress));
        }

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
}
