using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
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
[assembly: AssemblyVersion("1.4.0.0")]
[assembly: AssemblyFileVersion("1.4.0.0")]

namespace WPFTimer;

partial class MainWindow : Window {
    MediaPlayer _player = new MediaPlayer();

    MainViewModel ViewModel {
        get { return (MainViewModel)this.DataContext; }
    }
    
    public MainWindow() {
        InitializeComponent();

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

    void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (sender is MainViewModel model && nameof(MainViewModel.RingtoneEnabled).Equals(e.PropertyName)) {
            if (CheckAccess()) {
                EnableAlarmMode(model.RingtoneEnabled);
            }
            else Dispatcher.BeginInvoke(() => EnableAlarmMode(model.RingtoneEnabled));
        }
    }

    void EnableAlarmMode(bool enable) {
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
}


