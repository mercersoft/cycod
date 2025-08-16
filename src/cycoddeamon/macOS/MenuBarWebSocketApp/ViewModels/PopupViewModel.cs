using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media;
using MenuBarWebSocketApp.Services;

namespace MenuBarWebSocketApp.ViewModels;

public class PopupViewModel : INotifyPropertyChanged
{
    private readonly WebSocketServer _server;
    private string _port = "6464";
    private string _allowedOrigin = "https://example.com,http://localhost:5173";
    private string _authToken = "";
    private bool _autoStart = true;
    private string _activityLog = "";

    public PopupViewModel(WebSocketServer server)
    {
        _server = server;
        _server.OnActivityLogged += (message) => 
        {
            ActivityLog = $"[{DateTime.Now:HH:mm:ss}] {message}\n" + ActivityLog;
            if (ActivityLog.Length > 500)
                ActivityLog = ActivityLog.Substring(0, 500);
        };
        
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
        
        LoadCurrentSettings();
    }

    public string Port
    {
        get => _port;
        set { _port = value; OnPropertyChanged(); OnPropertyChanged(nameof(ServerUrl)); }
    }

    public string AllowedOrigin
    {
        get => _allowedOrigin;
        set { _allowedOrigin = value; OnPropertyChanged(); }
    }

    public string AuthToken
    {
        get => _authToken;
        set { _authToken = value; OnPropertyChanged(); }
    }

    public bool AutoStart
    {
        get => _autoStart;
        set { _autoStart = value; OnPropertyChanged(); }
    }

    public string ActivityLog
    {
        get => _activityLog;
        set { _activityLog = value; OnPropertyChanged(); }
    }

    public bool IsServerRunning => _server.IsRunning;
    public string ServerStatus => IsServerRunning ? "Running" : "Stopped";
    public string StatusText => IsServerRunning ? "● Running" : "● Stopped";
    public IBrush StatusColor => IsServerRunning ? Brushes.Green : Brushes.Gray;
    public int ActiveConnections => _server.ActiveConnections;
    public string ServerUrl => $"ws://localhost:{Port}";

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    
    public event Action? CloseRequested;

    private void LoadCurrentSettings()
    {
        Port = _server.Port.ToString();
        AllowedOrigin = _server.AllowedOrigin;
        AuthToken = _server.AuthToken;
    }

    private async void Save()
    {
        if (int.TryParse(Port, out var portNumber))
        {
            _server.Port = portNumber;
        }
        
        _server.AllowedOrigin = AllowedOrigin;
        _server.AuthToken = AuthToken;
        
        if (_server.IsRunning)
        {
            await _server.RestartAsync();
        }
        
        CloseRequested?.Invoke();
    }

    private void Cancel()
    {
        CloseRequested?.Invoke();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
}