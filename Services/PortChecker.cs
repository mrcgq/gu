


// =============================================================================
// 文件: Services/PortChecker.cs
// 描述: 端口占用检测服务 - 检查内核所需端口是否被占用
// =============================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace PhantomGUI.Services;

/// <summary>
/// 端口检测结果
/// </summary>
public class PortCheckResult
{
    public int Port { get; set; }
    public string Protocol { get; set; } = "TCP";
    public bool IsInUse { get; set; }
    public string? ProcessName { get; set; }
    public int? ProcessId { get; set; }
    public string Description { get; set; } = "";
    
    public string StatusMessage => IsInUse 
        ? $"端口 {Port} ({Protocol}) 被占用 - {ProcessName ?? "未知进程"} (PID: {ProcessId ?? 0})"
        : $"端口 {Port} ({Protocol}) 可用";
}

/// <summary>
/// 端口检测服务
/// </summary>
public static class PortChecker
{
    /// <summary>
    /// 检查内核所需的所有端口
    /// 对应 internal/config/config.go 中的默认端口配置
    /// </summary>
    public static List<PortCheckResult> CheckPhantomPorts(
        int mainPort = 54321,      // Listen
        int fakeTcpPort = 54322,   // FakeTCP
        int metricsPort = 9100,    // Metrics
        int wsPort = 54323,        // WebSocket
        bool checkFakeTcp = false,
        bool checkWebSocket = false)
    {
        var results = new List<PortCheckResult>();
        
        // 主 UDP 端口 (必须)
        results.Add(CheckPort(mainPort, "UDP", "主监听端口"));
        
        // Metrics 端口 (必须)
        results.Add(CheckPort(metricsPort, "TCP", "监控指标端口"));
        
        // FakeTCP 端口 (可选)
        if (checkFakeTcp)
        {
            results.Add(CheckPort(fakeTcpPort, "TCP", "FakeTCP 端口"));
        }
        
        // WebSocket 端口 (可选)
        if (checkWebSocket)
        {
            results.Add(CheckPort(wsPort, "TCP", "WebSocket 端口"));
        }
        
        return results;
    }
    
    /// <summary>
    /// 检查单个端口
    /// </summary>
    public static PortCheckResult CheckPort(int port, string protocol = "TCP", string description = "")
    {
        var result = new PortCheckResult
        {
            Port = port,
            Protocol = protocol,
            Description = description
        };
        
        try
        {
            if (protocol.Equals("UDP", StringComparison.OrdinalIgnoreCase))
            {
                result.IsInUse = IsUdpPortInUse(port);
            }
            else
            {
                result.IsInUse = IsTcpPortInUse(port);
            }
            
            // 如果端口被占用，尝试获取占用进程信息
            if (result.IsInUse)
            {
                var processInfo = GetProcessUsingPort(port, protocol);
                result.ProcessName = processInfo.name;
                result.ProcessId = processInfo.pid;
            }
        }
        catch (Exception ex)
        {
            // 检测失败时假设端口可用
            result.IsInUse = false;
            System.Diagnostics.Debug.WriteLine($"端口检测异常: {ex.Message}");
        }
        
        return result;
    }
    
    /// <summary>
    /// 检查 TCP 端口是否被占用
    /// </summary>
    public static bool IsTcpPortInUse(int port)
    {
        try
        {
            // 方法1：尝试绑定端口
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return false;
        }
        catch (SocketException)
        {
            return true;
        }
    }
    
    /// <summary>
    /// 检查 UDP 端口是否被占用
    /// </summary>
    public static bool IsUdpPortInUse(int port)
    {
        try
        {
            using var udpClient = new UdpClient(port);
            udpClient.Close();
            return false;
        }
        catch (SocketException)
        {
            return true;
        }
    }
    
    /// <summary>
    /// 使用 netstat 获取占用端口的进程信息
    /// </summary>
    private static (string? name, int? pid) GetProcessUsingPort(int port, string protocol)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = "-ano",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            
            using var process = Process.Start(startInfo);
            if (process == null) return (null, null);
            
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);
            
            var lines = output.Split('\n');
            var portStr = $":{port}";
            var protoLower = protocol.ToLower();
            
            foreach (var line in lines)
            {
                var lineLower = line.ToLower();
                if (lineLower.Contains(protoLower) && line.Contains(portStr))
                {
                    // 解析 PID (最后一列)
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 5 && int.TryParse(parts[^1], out int pid))
                    {
                        try
                        {
                            var proc = Process.GetProcessById(pid);
                            return (proc.ProcessName, pid);
                        }
                        catch
                        {
                            return (null, pid);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取进程信息失败: {ex.Message}");
        }
        
        return (null, null);
    }
    
    /// <summary>
    /// 使用 IPGlobalProperties 检查 TCP 端口 (更快但信息较少)
    /// </summary>
    public static bool IsTcpPortInUseFast(int port)
    {
        var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
        var tcpConnections = ipProperties.GetActiveTcpConnections();
        var tcpListeners = ipProperties.GetActiveTcpListeners();
        
        // 检查活动连接
        if (tcpConnections.Any(c => c.LocalEndPoint.Port == port))
            return true;
            
        // 检查监听端口
        if (tcpListeners.Any(l => l.Port == port))
            return true;
            
        return false;
    }
    
    /// <summary>
    /// 使用 IPGlobalProperties 检查 UDP 端口 (更快但信息较少)
    /// </summary>
    public static bool IsUdpPortInUseFast(int port)
    {
        var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
        var udpListeners = ipProperties.GetActiveUdpListeners();
        
        return udpListeners.Any(l => l.Port == port);
    }
    
    /// <summary>
    /// 生成端口冲突报告
    /// </summary>
    public static string GenerateConflictReport(List<PortCheckResult> results)
    {
        var conflicts = results.Where(r => r.IsInUse).ToList();
        
        if (conflicts.Count == 0)
            return "所有端口检测通过，可以启动。";
            
        var sb = new StringBuilder();
        sb.AppendLine("检测到以下端口冲突：\n");
        
        foreach (var conflict in conflicts)
        {
            sb.AppendLine($"  ❌ {conflict.StatusMessage}");
            if (!string.IsNullOrEmpty(conflict.Description))
            {
                sb.AppendLine($"     用途: {conflict.Description}");
            }
        }
        
        sb.AppendLine("\n解决方案：");
        sb.AppendLine("  1. 关闭占用端口的程序");
        sb.AppendLine("  2. 或在配置中修改端口号");
        
        // 检查是否是 Phantom 自身
        if (conflicts.Any(c => c.ProcessName?.Contains("phantom", StringComparison.OrdinalIgnoreCase) == true))
        {
            sb.AppendLine("\n提示: 检测到可能有另一个 Phantom 实例正在运行，请先关闭它。");
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// 查找可用端口 (从指定端口开始)
    /// </summary>
    public static int FindAvailablePort(int startPort, string protocol = "TCP", int maxAttempts = 100)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            var port = startPort + i;
            if (port > 65535) break;
            
            var isInUse = protocol.Equals("UDP", StringComparison.OrdinalIgnoreCase)
                ? IsUdpPortInUse(port)
                : IsTcpPortInUse(port);
                
            if (!isInUse)
                return port;
        }
        
        return -1; // 未找到可用端口
    }
}










