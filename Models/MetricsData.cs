// =============================================================================
// 文件: Models/MetricsData.cs
// 描述: 适配 internal/metrics/gauges.go 的监控数据模型
// =============================================================================
using System;
using System.Collections.Generic;

namespace PhantomGUI.Models;

/// <summary>
/// 实时监控数据 - 对应 internal/metrics/gauges.go 的 PhantomMetrics
/// </summary>
public class MetricsData
{
    // 连接相关
    public int ActiveConnections { get; set; }
    public long TotalConnections { get; set; }

    // 流量相关 (字节/秒)
    public double UploadSpeed { get; set; }
    public double DownloadSpeed { get; set; }
    public long TotalBytesSent { get; set; }
    public long TotalBytesReceived { get; set; }
    public long TotalPacketsSent { get; set; }
    public long TotalPacketsReceived { get; set; }

    // 延迟相关
    public double RTTMs { get; set; }
    public double AvgLatencyMs { get; set; }

    // ARQ 相关
    public long ARQRetransmits { get; set; }
    public int ARQWindowSize { get; set; }
    public double ARQPacketLoss { get; set; }

    // 拥塞控制
    public long CongestionWindow { get; set; }
    public double SendRate { get; set; }

    // 模式切换
    public string CurrentMode { get; set; } = "unknown";
    public string CurrentState { get; set; } = "unknown";
    public long TotalSwitches { get; set; }

    // 错误统计
    public long DecryptErrors { get; set; }
    public long ReplayAttacks { get; set; }
    public long AuthFailures { get; set; }

    // 时间戳
    public DateTime Timestamp { get; set; } = DateTime.Now;

    // 格式化显示
    public string UploadSpeedFormatted => FormatSpeed(UploadSpeed);
    public string DownloadSpeedFormatted => FormatSpeed(DownloadSpeed);
    public string RTTFormatted => $"{RTTMs:F1} ms";

    private static string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond >= 1_000_000_000)
            return $"{bytesPerSecond / 1_000_000_000:F2} GB/s";
        if (bytesPerSecond >= 1_000_000)
            return $"{bytesPerSecond / 1_000_000:F2} MB/s";
        if (bytesPerSecond >= 1_000)
            return $"{bytesPerSecond / 1_000:F2} KB/s";
        return $"{bytesPerSecond:F0} B/s";
    }
}

/// <summary>
/// 模式统计数据 - 对应 internal/metrics/collectors.go 的 ModeStatData
/// </summary>
public class ModeStatData
{
    public string Mode { get; set; } = "";
    public string State { get; set; } = "unknown";
    public long SwitchInCount { get; set; }
    public long SwitchOutCount { get; set; }
    public long FailureCount { get; set; }
    public double TotalTimeSec { get; set; }
    public double RTTMs { get; set; }
    public double LossRate { get; set; }
    public long TotalPackets { get; set; }
    public double Score { get; set; }
}

/// <summary>
/// 健康状态 - 对应 internal/metrics/server.go 的 HealthStatus
/// </summary>
public class HealthStatus
{
    public string Status { get; set; } = "unknown";
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = "";
    public TimeSpan Uptime { get; set; }
    public Dictionary<string, ComponentHealth> Components { get; set; } = new();

    public bool IsHealthy => Status == "healthy";
    public bool IsDegraded => Status == "degraded";
}

public class ComponentHealth
{
    public string Status { get; set; } = "";
    public string? Message { get; set; }
}



