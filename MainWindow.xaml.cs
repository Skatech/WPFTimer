using System;
using System.Globalization;
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
[assembly: AssemblyVersion("1.3.0.0")]
[assembly: AssemblyFileVersion("1.3.0.0")]

namespace WPFTimer;

partial class MainWindow : Window {
    MediaPlayer _player = new MediaPlayer();
    int _counter = 0;

    MainViewModel ViewModel {
        get { return (MainViewModel)this.DataContext; }
    }

    void EnablePlayer(bool enable) {
        if (enable) {
            if (Interlocked.Increment(ref _counter) == 1) {
                if (CheckAccess()) {
                    _player.Play();
                    Activate();
                }
                else {
                    Dispatcher.BeginInvoke(() => _player.Play());
                    Dispatcher.BeginInvoke(() => Activate());
                }
            }
        }
        else {
            if (Interlocked.Decrement(ref _counter) == 0) {
                if (CheckAccess()) {
                    _player.Stop();
                }
                else Dispatcher.BeginInvoke(() => _player.Stop());
            }
        }
    }

    public MainWindow() {
        InitializeComponent();
        
        _player.Open(new Uri("ringtone.mp3", UriKind.Relative));
        _player.Volume = 1.0;
        _player.MediaEnded += (_, _) => {
                _player.Position = TimeSpan.FromMilliseconds(1);
                _player.Play();
        };
        ViewModel.RingtoneRequested += EnablePlayer;

        foreach (var arg in Environment.GetCommandLineArgs()) {
            if (int.TryParse(arg, out int value) && value > 0 && value < 3600) {
                ViewModel.StartTimer(value);
            }
        }
    }
}


