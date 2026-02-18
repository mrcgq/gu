// =============================================================================
// 文件: Models/ServerProfile.cs
// 描述: 服务器配置文件模型 (支持多服务器管理)
// =============================================================================
using System;
using System.Collections.Generic;

namespace PhantomGUI.Models;

/// <summary>
/// 服务器配置文件
/// </summary>
public class ServerProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "New Server";
    public string ServerAddress { get; set; } = "";
    public int ServerPort { get; set; } = 54321;
    public string PSK { get; set; } = "";
    
    // 传输模式
    public string TransportMode { get; set; } = "auto";
    
    // TLS 配置
    public bool TLSEnabled { get; set; }
    public string TLSServerName { get; set; } = "www.microsoft.com";
    public string TLSFingerprint { get; set; } = "chrome";
    
    // Hysteria2 配置
    public int UploadMbps { get; set; } = 100;
    public int DownloadMbps { get; set; } = 100;
    
    // 本地代理配置
    public int LocalSocksPort { get; set; } = 1080;
    public int LocalHttpPort { get; set; } = 1081;
    
    // 高级配置
    public bool EnableARQ { get; set; } = true;
    public int TimeWindow { get; set; } = 30;
    
    // 元数据
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastUsedAt { get; set; }
    public long TotalBytesUsed { get; set; }
    
    // 验证
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ServerAddress) &&
               !string.IsNullOrWhiteSpace(PSK) &&
               ServerPort > 0 && ServerPort <= 65535 &&
               LocalSocksPort > 0 && LocalSocksPort <= 65535;
    }
}

/// <summary>
/// 应用配置
/// </summary>
public class AppSettings
{
    public string SelectedProfileId { get; set; } = "";
    public List<ServerProfile> Profiles { get; set; } = new();
    
    // 通用设置
    public bool AutoConnect { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool EnableSystemProxy { get; set; } = true;
    
    // 主题
    public string Theme { get; set; } = "Dark";
    public string AccentColor { get; set; } = "#2196F3";
    
    // 日志
    public string LogLevel { get; set; } = "info";
    public bool SaveLogs { get; set; } = true;
    public int MaxLogFiles { get; set; } = 7;
}





