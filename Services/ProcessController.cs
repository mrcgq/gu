// =============================================================================
// 文件: Services/ProcessController.cs
// 描述: 内核进程控制器 - 管理 phantom-core.exe 的生命周期
//       修复：增加端口占用检测和管理员权限检查
// =============================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using PhantomGUI.Models;

namespace PhantomGUI.Services;

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Disconnecting,
    Error
}

/// <summary>
/// 启动检查结果
/// </summary>
public class PreflightCheckResult
{
    public bool Success { get; set; }
    public bool HasPortConflicts { get; set; }
    public bool RequiresAdmin { get; set; }
    public bool HasAdmin { get; set; }
    public List<PortCheckResult> PortResults { get; set; } = new();
    public string Message { get; set; } = "";
    public string? SuggestedMode { get; set; }
}

public class ProcessController : IDisposable
{
    private readonly ConfigurationService _configService;
    private Process? _coreProcess;
    private CancellationTokenSource? _cts;
    
    public event Action<ConnectionState>? StateChanged;
    public event Action<string>? LogReceived;
    public event Action<string>? ErrorReceived;
    
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public bool IsRunning => _coreProcess != null && !_coreProcess.HasExited;
    public int? ProcessId => _coreProcess?.Id;
    
    // 当前使用的端口 (用于状态显示)
    public int CurrentMainPort { get; private set; }
    public int CurrentMetricsPort { get; private set; }
    
    public ProcessController(ConfigurationService configService)
    {
        _configService = configService;
    }
    
    /// <summary>
    /// 启动前检查 (端口占用 + 权限)
    /// </summary>
    public PreflightCheckResult PerformPreflightCheck(ServerProfile profile)
    {
        var result = new PreflightCheckResult { Success = true };
        
        // 1. 计算实际使用的端口
        int mainPort = profile.LocalSocksPort + 1000; // 内核端口 = 本地端口 + 1000
        int metricsPort = 9100;
        int fakeTcpPort = mainPort + 1;
        int wsPort = mainPort + 2;
        
        // 2. 检查端口占用
        var portResults = PortChecker.CheckPhantomPorts(
            mainPort: mainPort,
            metricsPort: metricsPort,
            fakeTcpPort: fakeTcpPort,
            wsPort: wsPort,
            checkFakeTcp: profile.TransportMode == "faketcp",
            checkWebSocket: profile.TransportMode == "websocket"
        );
        
        result.PortResults = portResults;
        result.HasPortConflicts = portResults.Any(p => p.IsInUse);
        
        if (result.HasPortConflicts)
        {
            result.Success = false;
            result.Message = PortChecker.GenerateConflictReport(portResults);
        }
        
        // 3. 检查权限需求
        result.RequiresAdmin = AdminPrivilege.RequiresAdmin(profile.TransportMode);
        result.HasAdmin = AdminPrivilege.IsRunningAsAdmin();
        
        if (result.RequiresAdmin && !result.HasAdmin)
        {
            result.Success = false;
            result.SuggestedMode = "udp";
            
            if (!result.HasPortConflicts)
            {
                result.Message = $"{profile.TransportMode.ToUpper()} 模式需要管理员权限。\n\n" +
                                 AdminPrivilege.GetPrivilegeDescription(profile.TransportMode);
            }
            else
            {
                result.Message += $"\n\n此外，{profile.TransportMode.ToUpper()} 模式还需要管理员权限。";
            }
        }
        
        // 保存端口信息
        CurrentMainPort = mainPort;
        CurrentMetricsPort = metricsPort;
        
        return result;
    }
    
    /// <summary>
    /// 显示预检查对话框
    /// </summary>
    /// <returns>true 如果用户选择继续，false 如果取消</returns>
    public async Task<(bool proceed, string? newMode)> ShowPreflightDialogAsync(
        PreflightCheckResult check, 
        ServerProfile profile)
    {
        // 端口冲突
        if (check.HasPortConflicts)
        {
            var portResult = MessageBox.Show(
                check.Message + "\n\n是否仍要尝试启动？（可能会失败）",
                "端口冲突警告",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
                
            if (portResult == MessageBoxResult.No)
            {
                return (false, null);
            }
        }
        
        // 权限不足
        if (check.RequiresAdmin && !check.HasAdmin)
        {
            var privResult = MessageBox.Show(
                $"当前选择的 {profile.TransportMode.ToUpper()} 模式需要管理员权限。\n\n" +
                $"{AdminPrivilege.GetPrivilegeDescription(profile.TransportMode)}\n\n" +
                "请选择操作：\n" +
                "• [是] 以管理员身份重启程序\n" +
                "• [否] 使用 UDP 模式继续（不需要特殊权限）\n" +
                "• [取消] 取消连接",
                "需要管理员权限",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);
                
            switch (privResult)
            {
                case MessageBoxResult.Yes:
                    // 尝试重启
                    if (AdminPrivilege.RestartAsAdmin())
                    {
                        Application.Current.Shutdown();
                        return (false, null);
                    }
                    else
                    {
                        MessageBox.Show(
                            "无法以管理员身份启动。将使用 UDP 模式继续。",
                            "提权失败",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        return (true, "udp");
                    }
                    
                case MessageBoxResult.No:
                    return (true, "udp");
                    
                default:
                    return (false, null);
            }
        }
        
        return (true, null);
    }
    
    /// <summary>
    /// 启动内核进程 (带预检查)
    /// </summary>
    public async Task<bool> StartAsync(ServerProfile profile)
    {
        if (IsRunning)
        {
            await StopAsync();
        }
        
        // 执行预检查
        var preflightCheck = PerformPreflightCheck(profile);
        
        if (!preflightCheck.Success)
        {
            var (proceed, newMode) = await ShowPreflightDialogAsync(preflightCheck, profile);
            
            if (!proceed)
            {
                LogReceived?.Invoke("用户取消了连接操作");
                return false;
            }
            
            // 如果建议使用新模式
            if (!string.IsNullOrEmpty(newMode))
            {
                LogReceived?.Invoke($"已切换到 {newMode.ToUpper()} 模式");
                profile.TransportMode = newMode;
            }
        }
        
        SetState(ConnectionState.Connecting);
        
        try
        {
            var corePath = _configService.GetCorePath();
            if (string.IsNullOrEmpty(corePath) || !File.Exists(corePath))
            {
                ErrorReceived?.Invoke("未找到 phantom-core.exe\n请确保内核文件位于程序目录中。");
                SetState(ConnectionState.Error);
                return false;
            }
            
            // 生成配置文件
            var configPath = _configService.SaveConfigFile(profile);
            LogReceived?.Invoke($"配置文件已生成: {configPath}");
            
            // 构建启动参数 (对应 cmd/phantom-server/main.go 的 flag 定义)
            var arguments = BuildArguments(configPath, profile);
            LogReceived?.Invoke($"启动参数: {arguments}");
            
            _cts = new CancellationTokenSource();
            
            _coreProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = corePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(corePath),
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                },
                EnableRaisingEvents = true
            };
            
            _coreProcess.OutputDataReceived += OnOutputDataReceived;
            _coreProcess.ErrorDataReceived += OnErrorDataReceived;
            _coreProcess.Exited += OnProcessExited;
            
            if (!_coreProcess.Start())
            {
                ErrorReceived?.Invoke("无法启动进程");
                SetState(ConnectionState.Error);
                return false;
            }
            
            _coreProcess.BeginOutputReadLine();
            _coreProcess.BeginErrorReadLine();
            
            LogReceived?.Invoke($"内核进程已启动, PID: {_coreProcess.Id}");
            LogReceived?.Invoke($"主端口: {CurrentMainPort}, 监控端口: {CurrentMetricsPort}");
            
            // 等待连接成功或超时
            var connected = await WaitForConnectionAsync(TimeSpan.FromSeconds(30));
            
            if (!connected)
            {
                if (_coreProcess.HasExited)
                {
                    ErrorReceived?.Invoke($"内核进程异常退出, 退出码: {_coreProcess.ExitCode}");
                }
                else
                {
                    ErrorReceived?.Invoke("连接超时，内核可能未正常启动");
                }
                
                await StopAsync();
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            ErrorReceived?.Invoke($"启动异常: {ex.Message}");
            SetState(ConnectionState.Error);
            return false;
        }
    }
    
    /// <summary>
    /// 构建命令行参数
    /// </summary>
    private string BuildArguments(string configPath, ServerProfile profile)
    {
        var arguments = new StringBuilder();
        arguments.Append($"-c \"{configPath}\"");
        
        // 传输模式
        if (!string.IsNullOrEmpty(profile.TransportMode) && profile.TransportMode != "auto")
        {
            arguments.Append($" --mode {profile.TransportMode}");
        }
        
        // TLS 配置
        if (profile.TLSEnabled)
        {
            arguments.Append(" --tls");
            
            if (!string.IsNullOrEmpty(profile.TLSServerName))
            {
                arguments.Append($" --tls-sni {profile.TLSServerName}");
            }
            
            if (!string.IsNullOrEmpty(profile.TLSFingerprint))
            {
                arguments.Append($" --tls-fingerprint {profile.TLSFingerprint}");
            }
        }
        
        return arguments.ToString();
    }
    
    /// <summary>
    /// 等待连接建立
    /// </summary>
    private async Task<bool> WaitForConnectionAsync(TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        
        while (sw.Elapsed < timeout)
        {
            if (_coreProcess == null || _coreProcess.HasExited)
            {
                return false;
            }
            
            if (State == ConnectionState.Connected)
            {
                return true;
            }
            
            await Task.Delay(100);
        }
        
        return State == ConnectionState.Connected;
    }
    
    /// <summary>
    /// 处理标准输出
    /// </summary>
    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data))
            return;
            
        LogReceived?.Invoke(e.Data);
        
        // 检测启动成功的标志
        // 对应 cmd/phantom-server/main.go 中的启动日志
        if (e.Data.Contains("智能链路切换器已启动") ||
            e.Data.Contains("Switcher started") ||
            e.Data.Contains("已启动, 当前模式"))
        {
            SetState(ConnectionState.Connected);
        }
        
        // 检测错误
        if (e.Data.Contains("[ERROR]") || e.Data.Contains("启动失败"))
        {
            // 不立即设置为错误状态，等待进程退出处理
        }
    }
    
    /// <summary>
    /// 处理标准错误
    /// </summary>
    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            ErrorReceived?.Invoke(e.Data);
            
            // 检测特定错误
            if (e.Data.Contains("address already in use") ||
                e.Data.Contains("端口已被占用"))
            {
                ErrorReceived?.Invoke("端口被占用，请检查是否有其他程序正在使用相同端口。");
            }
            
            if (e.Data.Contains("permission denied") ||
                e.Data.Contains("requires root") ||
                e.Data.Contains("权限不足"))
            {
                ErrorReceived?.Invoke("权限不足，请以管理员身份运行程序。");
            }
        }
    }
    
    /// <summary>
    /// 处理进程退出
    /// </summary>
    private void OnProcessExited(object? sender, EventArgs e)
    {
        var exitCode = _coreProcess?.ExitCode ?? -1;
        LogReceived?.Invoke($"内核进程退出, 退出码: {exitCode}");
        
        // 解释退出码
        var exitMessage = exitCode switch
        {
            0 => "正常退出",
            1 => "配置错误",
            2 => "权限不足",
            _ => $"异常退出 (代码: {exitCode})"
        };
        
        LogReceived?.Invoke($"退出原因: {exitMessage}");
        
        if (State != ConnectionState.Disconnecting)
        {
            SetState(ConnectionState.Error);
        }
        else
        {
            SetState(ConnectionState.Disconnected);
        }
    }
    
    /// <summary>
    /// 停止内核进程
    /// </summary>
    public async Task StopAsync()
    {
        if (!IsRunning)
        {
            SetState(ConnectionState.Disconnected);
            return;
        }
        
        SetState(ConnectionState.Disconnecting);
        LogReceived?.Invoke("正在停止内核...");
        
        _cts?.Cancel();
        
        try
        {
            if (_coreProcess != null && !_coreProcess.HasExited)
            {
                // 先尝试优雅关闭
                try
                {
                    // 发送 Ctrl+C 信号 (Windows)
                    if (!_coreProcess.CloseMainWindow())
                    {
                        // 如果没有主窗口，直接终止
                        _coreProcess.Kill();
                    }
                }
                catch
                {
                    _coreProcess.Kill();
                }
                
                // 等待进程退出
                var exited = await Task.Run(() => _coreProcess.WaitForExit(5000));
                
                if (!exited)
                {
                    LogReceived?.Invoke("进程未响应，强制终止");
                    _coreProcess.Kill();
                    await Task.Run(() => _coreProcess.WaitForExit(1000));
                }
            }
        }
        catch (Exception ex)
        {
            ErrorReceived?.Invoke($"停止异常: {ex.Message}");
        }
        finally
        {
            CleanupProcess();
            SetState(ConnectionState.Disconnected);
            LogReceived?.Invoke("内核已停止");
        }
    }
    
    /// <summary>
    /// 清理进程资源
    /// </summary>
    private void CleanupProcess()
    {
        if (_coreProcess != null)
        {
            _coreProcess.OutputDataReceived -= OnOutputDataReceived;
            _coreProcess.ErrorDataReceived -= OnErrorDataReceived;
            _coreProcess.Exited -= OnProcessExited;
            _coreProcess.Dispose();
            _coreProcess = null;
        }
        
        _cts?.Dispose();
        _cts = null;
    }
    
    private void SetState(ConnectionState state)
    {
        if (State != state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }
    }
    
    public void Dispose()
    {
        _cts?.Cancel();
        
        if (_coreProcess != null && !_coreProcess.HasExited)
        {
            try
            {
                _coreProcess.Kill();
            }
            catch { }
        }
        
        CleanupProcess();
    }
}



