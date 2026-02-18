// =============================================================================
// 文件: Services/MetricsParser.cs
// 描述: Prometheus 指标解析器 - 适配 internal/metrics/gauges.go
// =============================================================================
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PhantomGUI.Models;

namespace PhantomGUI.Services;

public class MetricsParser : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _metricsUrl;
    private readonly string _healthUrl;
    
    private Timer? _pollTimer;
    private MetricsData? _lastMetrics;
    private double _lastBytesSent;
    private double _lastBytesReceived;
    private DateTime _lastPollTime;
    
    public event Action<MetricsData>? MetricsUpdated;
    public event Action<HealthStatus>? HealthUpdated;
    public event Action<Exception>? ErrorOccurred;
    
    public MetricsData? LastMetrics => _lastMetrics;
    public bool IsPolling => _pollTimer != null;
    
    public MetricsParser(string host = "127.0.0.1", int port = 9100)
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
        _metricsUrl = $"http://{host}:{port}/metrics";
        _healthUrl = $"http://{host}:{port}/health";
    }
    
    /// <summary>
    /// 开始轮询
    /// </summary>
    public void StartPolling(int intervalMs = 1000)
    {
        StopPolling();
        _lastPollTime = DateTime.Now;
        _pollTimer = new Timer(async _ => await PollAsync(), null, 0, intervalMs);
    }
    
    /// <summary>
    /// 停止轮询
    /// </summary>
    public void StopPolling()
    {
        _pollTimer?.Dispose();
        _pollTimer = null;
    }
    
    /// <summary>
    /// 解析 Prometheus 格式的指标
    /// </summary>
    public async Task<MetricsData?> FetchMetricsAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(_metricsUrl);
            return ParsePrometheusMetrics(response);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
            return null;
        }
    }
    
    /// <summary>
    /// 获取健康状态
    /// </summary>
    public async Task<HealthStatus?> FetchHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(_healthUrl);
            return JsonSerializer.Deserialize<HealthStatus>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
            return null;
        }
    }
    
    private async Task PollAsync()
    {
        var metrics = await FetchMetricsAsync();
        if (metrics != null)
        {
            // 计算速度
            var now = DateTime.Now;
            var elapsed = (now - _lastPollTime).TotalSeconds;
            
            if (elapsed > 0 && _lastBytesSent > 0)
            {
                metrics.UploadSpeed = (metrics.TotalBytesSent - _lastBytesSent) / elapsed;
                metrics.DownloadSpeed = (metrics.TotalBytesReceived - _lastBytesReceived) / elapsed;
            }
            
            _lastBytesSent = metrics.TotalBytesSent;
            _lastBytesReceived = metrics.TotalBytesReceived;
            _lastPollTime = now;
            
            _lastMetrics = metrics;
            MetricsUpdated?.Invoke(metrics);
        }
        
        // 每 5 秒获取一次健康状态
        if (DateTime.Now.Second % 5 == 0)
        {
            var health = await FetchHealthAsync();
            if (health != null)
            {
                HealthUpdated?.Invoke(health);
            }
        }
    }
    
    /// <summary>
    /// 解析 Prometheus 文本格式
    /// 对应 internal/metrics/gauges.go 和 collectors.go 中的指标定义
    /// </summary>
    private MetricsData ParsePrometheusMetrics(string text)
    {
        var data = new MetricsData
        {
            Timestamp = DateTime.Now
        };
        
        var lines = text.Split('\n');
        
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;
                
            try
            {
                // 活跃连接数
                if (TryParseMetric(line, "phantom_active_connections", out var value))
                {
                    data.ActiveConnections = (int)value;
                }
                // 总发送字节数
                else if (TryParseMetricWithLabel(line, "phantom_bytes_sent_total", out value))
                {
                    data.TotalBytesSent = (long)value;
                }
                // 总接收字节数
                else if (TryParseMetricWithLabel(line, "phantom_bytes_received_total", out value))
                {
                    data.TotalBytesReceived = (long)value;
                }
                // 发送包数
                else if (TryParseMetricWithLabel(line, "phantom_packets_total", "direction=\"out\"", out value))
                {
                    data.TotalPacketsSent = (long)value;
                }
                // 接收包数
                else if (TryParseMetricWithLabel(line, "phantom_packets_total", "direction=\"in\"", out value))
                {
                    data.TotalPacketsReceived = (long)value;
                }
                // ARQ 重传
                else if (TryParseMetric(line, "phantom_arq_retransmits_total", out value))
                {
                    data.ARQRetransmits = (long)value;
                }
                // ARQ 窗口大小
                else if (TryParseMetric(line, "phantom_arq_window_size", out value))
                {
                    data.ARQWindowSize = (int)value;
                }
                // ARQ 丢包率
                else if (TryParseMetric(line, "phantom_arq_packet_loss_rate", out value))
                {
                    data.ARQPacketLoss = value;
                }
                // 拥塞窗口
                else if (TryParseMetric(line, "phantom_congestion_window_bytes", out value))
                {
                    data.CongestionWindow = (long)value;
                }
                // 发送速率
                else if (TryParseMetric(line, "phantom_congestion_send_rate_bytes_per_second", out value))
                {
                    data.SendRate = value;
                }
                // 当前模式
                else if (TryParseMetricWithLabel(line, "phantom_switcher_current_mode", out value) && value == 1)
                {
                    var match = Regex.Match(line, @"mode=""(\w+)""");
                    if (match.Success)
                    {
                        data.CurrentMode = match.Groups[1].Value;
                    }
                }
                // 当前状态
                else if (TryParseMetricWithLabel(line, "phantom_switcher_current_state", out value) && value == 1)
                {
                    var match = Regex.Match(line, @"state=""(\w+)""");
                    if (match.Success)
                    {
                        data.CurrentState = match.Groups[1].Value;
                    }
                }
                // RTT (模式级别)
                else if (TryParseMetric(line, "phantom_switcher_mode_rtt_milliseconds", out value) && value > 0)
                {
                    if (data.RTTMs == 0 || value < data.RTTMs)
                    {
                        data.RTTMs = value;
                    }
                }
                // 切换次数
                else if (TryParseMetric(line, "phantom_switcher_switches_total", out value))
                {
                    data.TotalSwitches = (long)value;
                }
                // 解密错误
                else if (TryParseMetric(line, "phantom_handler_decrypt_errors_total", out value))
                {
                    data.DecryptErrors = (long)value;
                }
                // 重放攻击
                else if (TryParseMetric(line, "phantom_handler_replay_attacks_total", out value))
                {
                    data.ReplayAttacks = (long)value;
                }
                // 认证失败
                else if (TryParseMetric(line, "phantom_handler_auth_failure_total", out value))
                {
                    data.AuthFailures = (long)value;
                }
            }
            catch
            {
                // 忽略解析错误
            }
        }
        
        return data;
    }
    
    private static bool TryParseMetric(string line, string name, out double value)
    {
        value = 0;
        if (!line.StartsWith(name))
            return false;
            
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return double.TryParse(parts[^1], out value);
        }
        return false;
    }
    
    private static bool TryParseMetricWithLabel(string line, string name, out double value)
    {
        value = 0;
        if (!line.StartsWith(name))
            return false;
            
        var match = Regex.Match(line, @"\}\s+([\d.eE+-]+)$");
        if (match.Success)
        {
            return double.TryParse(match.Groups[1].Value, out value);
        }
        return false;
    }
    
    private static bool TryParseMetricWithLabel(string line, string name, string label, out double value)
    {
        value = 0;
        if (!line.StartsWith(name) || !line.Contains(label))
            return false;
            
        var match = Regex.Match(line, @"\}\s+([\d.eE+-]+)$");
        if (match.Success)
        {
            return double.TryParse(match.Groups[1].Value, out value);
        }
        return false;
    }
    
    public void Dispose()
    {
        StopPolling();
        _httpClient.Dispose();
    }
}



