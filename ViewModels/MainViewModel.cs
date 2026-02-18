// =============================================================================
// 文件: ViewModels/MainViewModel.cs
// 描述: 主窗口视图模型 - MVVM 模式
//       修复：增加端口检测和权限检查的 UI 反馈
// =============================================================================
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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
    
    [ObservableProperty]
    private string _statusColor = "#808080";
    
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
    private string _currentMode = "unknown";
    
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
    public ICommand GeneratePSKCommand { get; }
    public ICommand CheckTimeSyncCommand { get; }
    public ICommand CheckPortsCommand { get; }
    public ICommand RestartAsAdminCommand { get; }
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
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, CanConnect);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, CanDisconnect);
        AddServerCommand = new RelayCommand(AddServer);
        EditServerCommand = new RelayCommand(EditServer, () => SelectedServer != null);
        DeleteServerCommand = new RelayCommand(DeleteServer, () => SelectedServer != null);
        GeneratePSKCommand = new RelayCommand<Action<string>>(GeneratePSK);
        CheckTimeSyncCommand = new AsyncRelayCommand(CheckTimeSyncAsync);
        CheckPortsCommand = new RelayCommand(CheckPorts);
        RestartAsAdminCommand = new RelayCommand(RestartAsAdmin);
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
            await CheckTimeSyncAsync();
        });
        
        // 处理自动连接
        if (App.StartupArgs.AutoConnect && Servers.Count > 0)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(500); // 等待 UI 加载
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    if (SelectedServer != null)
                    {
                        await ConnectAsync();
                    }
                });
            });
        }
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
        
        // 如果命令行指定了 profile
        if (!string.IsNullOrEmpty(App.StartupArgs.ProfileId))
        {
            SelectedServer = Servers.FirstOrDefault(s => 
                s.Id == App.StartupArgs.ProfileId || 
                s.Name == App.StartupArgs.ProfileId);
        }
        
        // 默认选择第一个
        SelectedServer ??= Servers.FirstOrDefault();
    }
    
    private bool CanConnect() => 
        ConnectionState == ConnectionState.Disconnected && 
        SelectedServer != null;
    
    private bool CanDisconnect() => 
        ConnectionState == ConnectionState.Connected ||
        ConnectionState == ConnectionState.Connecting;
    
    /// <summary>
    /// 连接 (带预检查)
    /// </summary>
    private async Task ConnectAsync()
    {
        if (SelectedServer == null) return;
        
        AddLog($"准备连接到 {SelectedServer.Name}...");
        
        // 检查是否需要特殊权限
        if (AdminPrivilege.RequiresAdmin(SelectedServer.TransportMode) && !IsAdmin)
        {
            AddLog($"⚠️ {SelectedServer.TransportMode.ToUpper()} 模式需要管理员权限");
        }
        
        // 启动内核 (ProcessController 内部会处理预检查)
        var success = await _processController.StartAsync(SelectedServer);
        
        if (success)
        {
            // 更新端口信息
            PortInfo = $"主端口: {_processController.CurrentMainPort}, 监控: {_processController.CurrentMetricsPort}";
            
            // 启动监控数据轮询
            _metricsParser.StartPolling(1000);
            
            // 设置系统代理
            if (_configService.Settings.EnableSystemProxy)
            {
                SystemProxyService.SetSocksProxy("127.0.0.1", SelectedServer.LocalSocksPort);
                AddLog($"✓ 已设置系统代理: 127.0.0.1:{SelectedServer.LocalSocksPort}");
            }
            
            // 更新服务器使用时间
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
        
        // 清除系统代理
        if (_configService.Settings.EnableSystemProxy)
        {
            SystemProxyService.ClearProxy();
            AddLog("✓ 已清除系统代理");
        }
        
        await _processController.StopAsync();
        
        PortInfo = "";
    }
    
    /// <summary>
    /// 检查端口占用
    /// </summary>
    private void CheckPorts()
    {
        if (SelectedServer == null)
        {
            MessageBox.Show("请先选择一个服务器配置", "提示", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        int mainPort = SelectedServer.LocalSocksPort + 1000;
        
        var results = PortChecker.CheckPhantomPorts(
            mainPort: mainPort,
            checkFakeTcp: SelectedServer.TransportMode == "faketcp",
            checkWebSocket: SelectedServer.TransportMode == "websocket"
        );
        
        var report = PortChecker.GenerateConflictReport(results);
        
        var hasConflicts = results.Any(r => r.IsInUse);
        
        MessageBox.Show(
            report,
            hasConflicts ? "端口冲突检测结果" : "端口检测通过",
            MessageBoxButton.OK,
            hasConflicts ? MessageBoxImage.Warning : MessageBoxImage.Information);
    }
    
    /// <summary>
    /// 以管理员身份重启
    /// </summary>
    private void RestartAsAdmin()
    {
        if (IsAdmin)
        {
            MessageBox.Show("程序已经以管理员身份运行", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        var result = MessageBox.Show(
            "是否以管理员身份重新启动程序？\n\n" +
            "这将关闭当前窗口并重新打开。",
            "确认重启",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
            
        if (result == MessageBoxResult.Yes)
        {
            if (AdminPrivilege.RestartAsAdmin())
            {
                Application.Current.Shutdown();
            }
            else
            {
                MessageBox.Show("无法以管理员身份启动", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
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
        // 打开编辑对话框
        // 这里需要配合 MaterialDesign DialogHost 使用
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
    
    private void GeneratePSK(Action<string>? callback)
    {
        var psk = ConfigurationService.GeneratePSK();
        callback?.Invoke(psk);
        AddLog("✓ 已生成新的 PSK 密钥");
    }
    
    private async Task CheckTimeSyncAsync()
    {
        var result = await TimeSyncService.CheckTimeSyncAsync();
        
        TimeSyncWarning = !result.IsSynced;
        TimeSyncMessage = result.Message;
        
        if (!result.IsSynced)
        {
            AddLog($"⚠️ 时间同步警告: {result.Message}");
            AddLog("提示: TSKD 认证要求客户端时间与服务器时间差在 30 秒以内");
        }
        else if (result.Success)
        {
            AddLog($"✓ 时间同步正常 (偏差: {result.OffsetSeconds:F1}秒)");
        }
    }
    
    private void OnStateChanged(ConnectionState state)
    {
        ConnectionState = state;
        
        (StatusText, StatusColor) = state switch
        {
            ConnectionState.Disconnected => ("未连接", "#808080"),
            ConnectionState.Connecting => ("连接中...", "#FFA500"),
            ConnectionState.Connected => ("已连接", "#4CAF50"),
            ConnectionState.Disconnecting => ("断开中...", "#FFA500"),
            ConnectionState.Error => ("连接错误", "#F44336"),
            _ => ("未知", "#808080")
        };
        
        // 更新命令状态
        ((AsyncRelayCommand)ConnectCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)DisconnectCommand).NotifyCanExecuteChanged();
    }
    
    private void OnMetricsUpdated(MetricsData metrics)
    {
        Metrics = metrics;
        UploadSpeed = metrics.UploadSpeedFormatted;
        DownloadSpeed = metrics.DownloadSpeedFormatted;
        Rtt = metrics.RTTFormatted;
        CurrentMode = metrics.CurrentMode;
        ActiveConnections = metrics.ActiveConnections;
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







