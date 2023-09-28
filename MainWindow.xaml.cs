using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;

[assembly:ThemeInfo(
    ResourceDictionaryLocation.None,
    ResourceDictionaryLocation.SourceAssembly
)]

[assembly: AssemblyTitle("WPF Timer")]
[assembly: AssemblyDescription("WPF Timer")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Skatech")]
[assembly: AssemblyProduct("WPFTimer")]
[assembly: AssemblyCopyright("Copyright Skatech Lab 2023")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyVersion("1.6.0.0")]
[assembly: AssemblyFileVersion("1.6.0.0")]

namespace WPFTimer;

partial class MainWindow : Window {
    readonly MediaPlayer _player = new MediaPlayer();
    System.Windows.Threading.DispatcherTimer _timer = new();

    MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;
    
    public MainWindow() {
        InitializeComponent();

        _timer.Tick += new EventHandler(OnTimerTick); 
        _timer.Interval = TimeSpan.FromMilliseconds(1000); 
        _timer.Start(); 

        _player.Open(new Uri("ringtone.mp3", UriKind.Relative));
        _player.Volume = 1.0;
        _player.MediaEnded += (_, _) => {
                _player.Position = TimeSpan.FromMilliseconds(1);
                _player.Play();
        };

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        foreach (var arg in Environment.GetCommandLineArgs()) {
            if (int.TryParse(arg, out int value) && value > 0 && value < 3600) {
                ViewModel.StartTimer(value);
            }
        }
    }

    void SwitchAlarmMode(bool enable) {
        if (enable) {
            WindowState = WindowState.Normal;
            Topmost = true;
            Activate();
            _player.Play();
        }
        else {
            Topmost = false;
            _player.Stop();
        }
    }

    void OnTimerTick(object? sender, EventArgs e) {
        _timer.Interval = TimeSpan.FromMilliseconds((1490 - DateTime.Now.Millisecond) % 1000);
        ViewModel.Update();
    }

    void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (sender is MainWindowViewModel model &&
                nameof(MainWindowViewModel.RingtoneEnabled).Equals(e.PropertyName)) {
            SwitchAlarmMode(model.RingtoneEnabled);
        }
    }
}
