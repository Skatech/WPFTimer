using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Input;
using System.Diagnostics;
using System.Threading;
using System.Windows.Shell;

namespace WPFTimer;

class MainWindowViewModel : ObservableObject {
    public ObservableCollection<TimerView> Timers { get; } = new();
    public IEnumerable<StartButtonViewModel> Intervals { get; }
    public TaskbarItemProgressState TaskbarProgressState { get; private set; } = TaskbarItemProgressState.None;
    public double TaskbarProgressValue { get; private set; } = 0;
    public bool RingtoneEnabled { get; private set; }

    public MainWindowViewModel() {
        var command = new RelayCommand<int>(StartTimer);
        Intervals = new[] { 5, 10, 15, 20, 25, 30, 40, 50, 60, 90, 120, 180 }
            .Select(it => new StartButtonViewModel(it, command)).ToArray();
    }

    public void StartTimer(int minutes) {
        Timers.Add(new TimerView(this, TimeSpan.FromMinutes(minutes)));
    }

    public bool StopTimer(TimerView timer) {
        return Timers.Remove(timer);
    }

    public void Update() {
        var progress = 0.0;
        foreach(var timer in Timers) {
            timer.Update();
            progress = Math.Max(progress, timer.GetProgress());
        }

        if (progress == 0 || progress == 1 || TaskbarProgressValue == 0 ||
                Math.Abs(progress - TaskbarProgressValue) >= 0.001) {
            SetProperty(TaskbarProgressValue, progress,
                v => TaskbarProgressValue = v, nameof(TaskbarProgressValue));
        }

        SetProperty(TaskbarProgressState,
            Timers.Count > 0 ? TaskbarItemProgressState.Normal : TaskbarItemProgressState.None,
            v => TaskbarProgressState = v, nameof(TaskbarProgressState));

        SetProperty(RingtoneEnabled, progress == 1.0, v => RingtoneEnabled = v, nameof(RingtoneEnabled));
    }
}

class StartButtonViewModel {
    public int MinutesDelay { get; private set; }
    public ICommand StartCommand { get; private set; }

    public StartButtonViewModel(int minutesDelay, ICommand command) {
        MinutesDelay = minutesDelay;
        StartCommand = command;
    }
}

class TimerView : ObservableObject {
    public ICommand StopCommand => new RelayCommand(Stop);
    public TimeSpan Delay { get; }
    public TimeSpan Lasts { get; private set; }
    public Brush ForegroundBrush { get; private set; } = Brushes.Black;

    readonly MainWindowViewModel _model;
    readonly Stopwatch _timer = Stopwatch.StartNew();

    public TimerView(MainWindowViewModel model, TimeSpan delay) {
        _model = model; Lasts = Delay = delay;
    }

    public double GetProgress() {
        return 1.0 - Lasts / Delay;
    }

    public void Update() {
        if (Lasts > TimeSpan.Zero) {
            SetProperty(Lasts, Delay > _timer.Elapsed ? Delay - _timer.Elapsed : TimeSpan.Zero,
                v => Lasts = v, nameof(Lasts));
        }

        if (Lasts == TimeSpan.Zero) {
            SetProperty(ForegroundBrush, DateTime.Now.Second % 2 > 0 ? Brushes.Black : Brushes.Red,
                v => ForegroundBrush = v, nameof(ForegroundBrush));
        }
    }

    void Stop() {
        _model.StopTimer(this);
    }
}
