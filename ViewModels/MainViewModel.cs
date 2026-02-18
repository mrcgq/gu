// =============================================================================
// 文件: ViewModels/MainViewModel.cs
// 描述: 主窗口视图模型 - MVVM 模式 (修复版)
// =============================================================================
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhantomGUI.Models;
using PhantomGUI.Services;

namespace PhantomGUI.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ConfigurationService _configService;
    private readonly ProcessController _processController;
    private readonly MetricsParser _metricsParser;
    
    // ====== 连接状态 ======
    [ObservableProperty]
    private ConnectionState _connectionState = ConnectionState.Disconnected;
    
    [ObservableProperty]
    private string _statusText = "未连接";
    
    // 使用 Brush 而不是 Color 字符串
    [ObservableProperty]
    private Brush _statusBrush = new SolidColorBrush(Color.FromRgb(128, 128, 128));
    
    // 按钮可见性控制
    public bool CanConnect => ConnectionState == ConnectionState.Disconnected || 
                              ConnectionState == ConnectionState.Error;
    public bool CanDisconnect => ConnectionState == ConnectionState.Connected || 
                                 ConnectionState == ConnectionState.Connecting;
    
    // ====== 权限状态 ======
    [ObservableProperty]
    private bool _isAdmin;
    
    [ObservableProperty]
    private string _privilegeLevel = "";
    
    // ====== 服务器列表 ======
    [ObservableProperty]
    private ObservableCollection<ServerProfile> _servers = new();
    
    [ObservableProperty]
    private ServerProfile? _selectedServer;
    
    // ====== 实时数据 ======
    [ObservableProperty]
    private MetricsData? _metrics;
    
    [ObservableProperty]
    private string _uploadSpeed = "0 B/s";
    
    [ObservableProperty]
    private string _downloadSpeed = "0 B/s";
    
    [ObservableProperty]
    private string _rtt = "-- ms";
    
    [ObservableProperty]
    private string _currentMode = "N/A";
    
    [ObservableProperty]
    private int _activeConnections;
    
    // ====== 时间同步 ======
    [ObservableProperty]
    private bool _timeSyncWarning;
    
    [ObservableProperty]
    private string _timeSyncMessage = "";
    
    // ====== 端口信息 ======
    [ObservableProperty]
    private string _portInfo = "";
    
    // ====== 日志 ======
    [ObservableProperty]
    private ObservableCollection<string> _logs = new();
    
    // ====== 命令 ======
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand AddServerCommand { get; }
    public ICommand EditServerCommand { get; }
    public ICommand DeleteServerCommand { get; }
    public ICommand CheckTimeSyncCommand { get; }
    public ICommand ClearLogsCommand { get; }
    
    public MainViewModel()
    {
        _configService = new ConfigurationService();
        _processController = new ProcessController(_configService);
        _metricsParser = new MetricsParser();
        
        // 初始化权限状态
        IsAdmin = AdminPrivilege.IsRunningAsAdmin();
        PrivilegeLevel = AdminPrivilege.GetCurrentPrivilegeLevel();
        
        // 初始化命令
        ConnectCommand = new AsyncRelayCommand(ConnectAsync);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync);
        AddServerCommand = new RelayCommand(AddServer);
        EditServerCommand = new RelayCommand(EditServer);
        DeleteServerCommand = new RelayCommand(DeleteServer);
        CheckTimeSyncCommand = new AsyncRelayCommand(CheckTimeSyncAsync);
        ClearLogsCommand = new RelayCommand(() => Logs.Clear());
        
        // 事件订阅
        _processController.StateChanged += OnStateChanged;
        _processController.LogReceived += OnLogReceived;
        _processController.ErrorReceived += OnErrorReceived;
        _metricsParser.MetricsUpdated += OnMetricsUpdated;
        
        // 加载数据
        LoadServers();
        
        // 启动时检查
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            await CheckTimeSyncAsync();
        });
    }
    
    private void LoadServers()
    {
        Servers.Clear();
        foreach (var server in _configService.Settings.Profiles)
        {
            Servers.Add(server);
        }
        
        // 选择上次使用的服务器
        if (!string.IsNullOrEmpty(_configService.Settings.SelectedProfileId))
        {
            SelectedServer = Servers.FirstOrDefault(s => 
                s.Id == _configService.Settings.SelectedProfileId);
        }
        
        // 默认选择第一个
        SelectedServer ??= Servers.FirstOrDefault();
    }
    
    /// <summary>
    /// 连接
    /// </summary>
    private async Task ConnectAsync()
    {
        if (SelectedServer == null)
        {
            AddLog("请先选择一个服务器");
            return;
        }
        
        AddLog($"准备连接到 {SelectedServer.Name}...");
        
        var success = await _processController.StartAsync(SelectedServer);
        
        if (success)
        {
            PortInfo = $"端口: {_processController.CurrentMainPort}";
            _metricsParser.StartPolling(1000);
            
            if (_configService.Settings.EnableSystemProxy)
            {
                SystemProxyService.SetSocksProxy("127.0.0.1", SelectedServer.LocalSocksPort);
                AddLog($"✓ 已设置系统代理: 127.0.0.1:{SelectedServer.LocalSocksPort}");
            }
            
            SelectedServer.LastUsedAt = DateTime.Now;
            _configService.Settings.SelectedProfileId = SelectedServer.Id;
            _configService.SaveSettings();
            
            AddLog("✓ 连接成功!");
        }
        else
        {
            AddLog("✗ 连接失败");
        }
    }
    
    private async Task DisconnectAsync()
    {
        AddLog("正在断开连接...");
        
        _metricsParser.StopPolling();
        
        if (_configService.Settings.EnableSystemProxy)
        {
            SystemProxyService.ClearProxy();
            AddLog("✓ 已清除系统代理");
        }
        
        await _processController.StopAsync();
        
        PortInfo = "";
    }
    
    private void AddServer()
    {
        var newServer = new ServerProfile
        {
            Name = $"Server {Servers.Count + 1}",
            PSK = ConfigurationService.GeneratePSK()
        };
        
        Servers.Add(newServer);
        _configService.Settings.Profiles.Add(newServer);
        _configService.SaveSettings();
        
        SelectedServer = newServer;
        AddLog($"已添加新服务器: {newServer.Name}");
    }
    
    private void EditServer()
    {
        if (SelectedServer == null)
        {
            AddLog("请先选择一个服务器");
            return;
        }
        // TODO: 打开编辑对话框
        AddLog($"编辑服务器: {SelectedServer.Name}");
    }
    
    private void DeleteServer()
    {
        if (SelectedServer == null) return;
        
        var result = MessageBox.Show(
            $"确定要删除服务器 \"{SelectedServer.Name}\" 吗？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
            
        if (result == MessageBoxResult.Yes)
        {
            var name = SelectedServer.Name;
            _configService.Settings.Profiles.Remove(SelectedServer);
            Servers.Remove(SelectedServer);
            _configService.SaveSettings();
            
            SelectedServer = Servers.FirstOrDefault();
            AddLog($"已删除服务器: {name}");
        }
    }
    
    private async Task CheckTimeSyncAsync()
    {
        try
        {
            var result = await TimeSyncService.CheckTimeSyncAsync();
            
            Application.Current?.Dispatcher.Invoke(() =>
            {
                TimeSyncWarning = !result.IsSynced;
                TimeSyncMessage = result.Message;
                
                if (!result.IsSynced)
                {
                    AddLog($"⚠️ 时间同步警告: {result.Message}");
                }
                else if (result.Success)
                {
                    AddLog($"✓ 时间同步正常 (偏差: {result.OffsetSeconds:F1}秒)");
                }
            });
        }
        catch (Exception ex)
        {
            AddLog($"时间同步检查失败: {ex.Message}");
        }
    }
    
    private void OnStateChanged(ConnectionState state)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            ConnectionState = state;
            
            // 更新状态文本和颜色
            switch (state)
            {
                case ConnectionState.Disconnected:
                    StatusText = "未连接";
                    StatusBrush = new SolidColorBrush(Color.FromRgb(128, 128, 128)); // 灰色
                    break;
                case ConnectionState.Connecting:
                    StatusText = "连接中...";
                    StatusBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // 橙色
                    break;
                case ConnectionState.Connected:
                    StatusText = "已连接";
                    StatusBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // 绿色
                    break;
                case ConnectionState.Disconnecting:
                    StatusText = "断开中...";
                    StatusBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // 橙色
                    break;
                case ConnectionState.Error:
                    StatusText = "连接错误";
                    StatusBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // 红色
                    break;
            }
            
            // 通知按钮可见性变化
            OnPropertyChanged(nameof(CanConnect));
            OnPropertyChanged(nameof(CanDisconnect));
        });
    }
    
    private void OnMetricsUpdated(MetricsData metrics)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            Metrics = metrics;
            UploadSpeed = metrics.UploadSpeedFormatted;
            DownloadSpeed = metrics.DownloadSpeedFormatted;
            Rtt = metrics.RTTFormatted;
            CurrentMode = metrics.CurrentMode;
            ActiveConnections = metrics.ActiveConnections;
        });
    }
    
    private void OnLogReceived(string log)
    {
        AddLog(log);
    }
    
    private void OnErrorReceived(string error)
    {
        AddLog($"[ERROR] {error}");
    }
    
    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var log = $"[{timestamp}] {message}";
        
        Application.Current?.Dispatcher.Invoke(() =>
        {
            Logs.Add(log);
            
            // 限制日志数量
            while (Logs.Count > 1000)
            {
                Logs.RemoveAt(0);
            }
        });
    }
    
    public void Dispose()
    {
        _processController.Dispose();
        _metricsParser.Dispose();
    }
}
