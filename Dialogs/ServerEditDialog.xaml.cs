// =============================================================================
// 文件: Dialogs/ServerEditDialog.xaml.cs
// 描述: 服务器编辑对话框代码
// =============================================================================
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;
using PhantomGUI.Models;
using PhantomGUI.Services;

namespace PhantomGUI.Dialogs;

public partial class ServerEditDialog : UserControl, INotifyPropertyChanged
{
    private ServerProfile _profile;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public ICommand GeneratePSKCommand { get; }
    
    public ServerEditDialog(ServerProfile profile)
    {
        InitializeComponent();
        DataContext = this;
        
        _profile = profile;
        
        GeneratePSKCommand = new RelayCommand(ExecuteGeneratePSK);
        
        // 复制属性
        ServerName = profile.Name;
        ServerAddress = profile.ServerAddress;
        ServerPort = profile.ServerPort;
        PSK = profile.PSK;
        TransportMode = profile.TransportMode;
        UploadMbps = profile.UploadMbps;
        DownloadMbps = profile.DownloadMbps;
        TLSEnabled = profile.TLSEnabled;
        TLSServerName = profile.TLSServerName;
        TLSFingerprint = profile.TLSFingerprint;
        LocalSocksPort = profile.LocalSocksPort;
        LocalHttpPort = profile.LocalHttpPort;
        EnableARQ = profile.EnableARQ;
        TimeWindow = profile.TimeWindow;
    }
    
    // 属性绑定 - 修改为 ServerName 避免与基类冲突
    private string _serverName = "";
    public string ServerName
    {
        get => _serverName;
        set { _serverName = value; OnPropertyChanged(); }
    }
    
    private string _serverAddress = "";
    public string ServerAddress
    {
        get => _serverAddress;
        set { _serverAddress = value; OnPropertyChanged(); }
    }
    
    private int _serverPort = 54321;
    public int ServerPort
    {
        get => _serverPort;
        set { _serverPort = value; OnPropertyChanged(); }
    }
    
    private string _psk = "";
    public string PSK
    {
        get => _psk;
        set 
        { 
            _psk = value; 
            OnPropertyChanged();
            OnPropertyChanged(nameof(UserId));
        }
    }
    
    private string _transportMode = "auto";
    public string TransportMode
    {
        get => _transportMode;
        set { _transportMode = value; OnPropertyChanged(); }
    }
    
    private int _uploadMbps = 100;
    public int UploadMbps
    {
        get => _uploadMbps;
        set { _uploadMbps = value; OnPropertyChanged(); }
    }
    
    private int _downloadMbps = 100;
    public int DownloadMbps
    {
        get => _downloadMbps;
        set { _downloadMbps = value; OnPropertyChanged(); }
    }
    
    private bool _tlsEnabled;
    public bool TLSEnabled
    {
        get => _tlsEnabled;
        set { _tlsEnabled = value; OnPropertyChanged(); }
    }
    
    private string _tlsServerName = "www.microsoft.com";
    public string TLSServerName
    {
        get => _tlsServerName;
        set { _tlsServerName = value; OnPropertyChanged(); }
    }
    
    private string _tlsFingerprint = "chrome";
    public string TLSFingerprint
    {
        get => _tlsFingerprint;
        set { _tlsFingerprint = value; OnPropertyChanged(); }
    }
    
    private int _localSocksPort = 1080;
    public int LocalSocksPort
    {
        get => _localSocksPort;
        set { _localSocksPort = value; OnPropertyChanged(); }
    }
    
    private int _localHttpPort = 1081;
    public int LocalHttpPort
    {
        get => _localHttpPort;
        set { _localHttpPort = value; OnPropertyChanged(); }
    }
    
    private bool _enableARQ = true;
    public bool EnableARQ
    {
        get => _enableARQ;
        set { _enableARQ = value; OnPropertyChanged(); }
    }
    
    private int _timeWindow = 30;
    public int TimeWindow
    {
        get => _timeWindow;
        set { _timeWindow = value; OnPropertyChanged(); }
    }
    
    // 计算属性：UserID
    public string UserId => ConfigurationService.CalculateUserId(PSK);
    
    // 命令：生成 PSK
    private void ExecuteGeneratePSK()
    {
        PSK = ConfigurationService.GeneratePSK();
    }
    
    // 保存到 Profile
    public void SaveToProfile()
    {
        _profile.Name = ServerName;
        _profile.ServerAddress = ServerAddress;
        _profile.ServerPort = ServerPort;
        _profile.PSK = PSK;
        _profile.TransportMode = TransportMode;
        _profile.UploadMbps = UploadMbps;
        _profile.DownloadMbps = DownloadMbps;
        _profile.TLSEnabled = TLSEnabled;
        _profile.TLSServerName = TLSServerName;
        _profile.TLSFingerprint = TLSFingerprint;
        _profile.LocalSocksPort = LocalSocksPort;
        _profile.LocalHttpPort = LocalHttpPort;
        _profile.EnableARQ = EnableARQ;
        _profile.TimeWindow = TimeWindow;
    }
    
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

// 简单的 RelayCommand 实现
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
}
