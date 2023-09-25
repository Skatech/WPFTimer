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

class MainWindowViewModel : ObservableObject, IDisposable {
    public ObservableCollection<TimerView> Tasks { get; } = new();
    public IEnumerable<StartButtonViewModel> Intervals { get; }
    public TaskbarItemProgressState TaskbarProgressState { get; private set; } = TaskbarItemProgressState.None;
    public double TaskbarProgressValue { get; private set; } = 0;
    public bool RingtoneEnabled { get; private set; }
    readonly CancellationTokenSource _cts = new();
    readonly Task _worker;

    ~MainWindowViewModel() => Dispose();

    public MainWindowViewModel() {
        _worker = Worker();
        var command = new RelayCommand<int>(StartTimer);
        Intervals = new[] { 5, 10, 15, 20, 25, 30, 40, 50, 60, 90, 120, 180 }
            .Select(it => new StartButtonViewModel(it, command)).ToArray();
    }

    public void Dispose() {
        _cts.Cancel();
        Task.WhenAll(_worker);
        GC.SuppressFinalize(this);
    }

    public void StartTimer(int minutes) {
        Tasks.Add(new TimerView(this, TimeSpan.FromMinutes(minutes)));
    }

    public bool StopTimer(TimerView timer) {
        return Tasks.Remove(timer);
    }

    async Task Worker() {
        while(true) {
            await Task.Delay(1490 - DateTime.Now.Millisecond, _cts.Token).ConfigureAwait(true);
            var progress = 0.0;
            foreach(var timer in Tasks) {
                timer.Update();
                progress = Math.Max(progress, timer.GetProgress());
            }

            if (progress == 0 || progress == 1 || TaskbarProgressValue == 0 ||
                    Math.Abs(progress - TaskbarProgressValue) >= 0.001) {
                SetProperty(TaskbarProgressValue, progress,
                    v => TaskbarProgressValue = v, nameof(TaskbarProgressValue));
            }

            SetProperty(TaskbarProgressState,
                Tasks.Count > 0 ? TaskbarItemProgressState.Normal : TaskbarItemProgressState.None,
                v => TaskbarProgressState = v, nameof(TaskbarProgressState));

            SetProperty(RingtoneEnabled, progress == 1.0, v => RingtoneEnabled = v, nameof(RingtoneEnabled));
        }
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
