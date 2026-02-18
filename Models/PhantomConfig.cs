// =============================================================================
// 文件: Models/PhantomConfig.cs
// 描述: 完整适配 internal/config/config.go 的配置模型
//       修复：新增客户端专用配置，确保与 Go 端完美对接
// =============================================================================
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace PhantomGUI.Models;

/// <summary>
/// 主配置 - 精确对应 internal/config/config.go 的 Config 结构体
/// 同时支持服务端和客户端配置
/// </summary>
public class PhantomConfig
{
    // ==========================================================================
    // 服务端配置 (当作为服务端运行时使用)
    // ==========================================================================
    
    [YamlMember(Alias = "listen")]
    public string? Listen { get; set; }

    [YamlMember(Alias = "psk")]
    public string PSK { get; set; } = "";

    [YamlMember(Alias = "time_window")]
    public int TimeWindow { get; set; } = 30;

    [YamlMember(Alias = "log_level")]
    public string LogLevel { get; set; } = "info";

    [YamlMember(Alias = "mode")]
    public string Mode { get; set; } = "auto";

    // ==========================================================================
    // 客户端专用配置 (GUI 主要使用这部分)
    // 对应 Go 端 internal/handler/client_handler.go 的 Config 结构体
    // ==========================================================================
    
    /// <summary>
    /// 服务器地址 (客户端连接目标)
    /// 对应 Go 端: server_addr
    /// </summary>
    [YamlMember(Alias = "server_addr")]
    public string? ServerAddr { get; set; }
    
    /// <summary>
    /// 服务器端口
    /// 对应 Go 端: server_port
    /// </summary>
    [YamlMember(Alias = "server_port")]
    public int ServerPort { get; set; }
    
    /// <summary>
    /// 本地 SOCKS5 监听地址
    /// 对应 Go 端: socks_addr 或 socks5_listen
    /// </summary>
    [YamlMember(Alias = "socks_addr")]
    public string? SocksAddr { get; set; }
    
    /// <summary>
    /// 本地 HTTP 代理监听地址 (可选)
    /// 对应 Go 端: http_addr
    /// </summary>
    [YamlMember(Alias = "http_addr")]
    public string? HttpAddr { get; set; }
    
    /// <summary>
    /// 传输模式
    /// 对应 Go 端: transport_mode
    /// </summary>
    [YamlMember(Alias = "transport_mode")]
    public string? TransportMode { get; set; }

    // ==========================================================================
    // 子配置模块
    // ==========================================================================

    [YamlMember(Alias = "tunnel")]
    public TunnelConfig? Tunnel { get; set; }

    [YamlMember(Alias = "hysteria2")]
    public Hysteria2Config? Hysteria2 { get; set; }

    [YamlMember(Alias = "faketcp")]
    public FakeTCPConfig? FakeTCP { get; set; }

    [YamlMember(Alias = "websocket")]
    public WebSocketConfig? WebSocket { get; set; }

    [YamlMember(Alias = "ebpf")]
    public EBPFConfig? EBPF { get; set; }

    [YamlMember(Alias = "switcher")]
    public SwitcherConfig? Switcher { get; set; }

    [YamlMember(Alias = "metrics")]
    public MetricsConfig? Metrics { get; set; }

    [YamlMember(Alias = "arq")]
    public ARQConfig? ARQ { get; set; }

    [YamlMember(Alias = "tls")]
    public TLSConfig? TLS { get; set; }
    
    // ==========================================================================
    // 客户端专用：连接配置
    // ==========================================================================
    
    [YamlMember(Alias = "client")]
    public ClientConfig? Client { get; set; }
}

/// <summary>
/// 客户端专用配置
/// 对应 Go 端 internal/handler/client_handler.go 的 Config 结构体
/// </summary>
public class ClientConfig
{
    /// <summary>
    /// 服务器地址
    /// </summary>
    [YamlMember(Alias = "server_addr")]
    public string ServerAddr { get; set; } = "";
    
    /// <summary>
    /// 服务器端口
    /// </summary>
    [YamlMember(Alias = "server_port")]
    public int ServerPort { get; set; } = 54321;
    
    /// <summary>
    /// PSK 密钥
    /// </summary>
    [YamlMember(Alias = "psk")]
    public string PSK { get; set; } = "";
    
    /// <summary>
    /// 时间窗口 (秒)
    /// </summary>
    [YamlMember(Alias = "time_window")]
    public int TimeWindow { get; set; } = 30;
    
    /// <summary>
    /// 本地 SOCKS5 监听地址
    /// 格式: "127.0.0.1:1080" 或 ":1080"
    /// </summary>
    [YamlMember(Alias = "socks_addr")]
    public string SocksAddr { get; set; } = "127.0.0.1:1080";
    
    /// <summary>
    /// 本地 HTTP 代理监听地址 (可选)
    /// 格式: "127.0.0.1:1081" 或 ":1081"
    /// </summary>
    [YamlMember(Alias = "http_addr")]
    public string? HttpAddr { get; set; }
    
    /// <summary>
    /// 传输模式: auto, udp, faketcp, websocket
    /// </summary>
    [YamlMember(Alias = "transport_mode")]
    public string TransportMode { get; set; } = "auto";
    
    /// <summary>
    /// 上行带宽 (Mbps)
    /// </summary>
    [YamlMember(Alias = "up_mbps")]
    public int UpMbps { get; set; } = 100;
    
    /// <summary>
    /// 下行带宽 (Mbps)
    /// </summary>
    [YamlMember(Alias = "down_mbps")]
    public int DownMbps { get; set; } = 100;
    
    /// <summary>
    /// 启用 ARQ 可靠传输
    /// </summary>
    [YamlMember(Alias = "enable_arq")]
    public bool EnableARQ { get; set; } = true;
    
    /// <summary>
    /// TLS 配置
    /// </summary>
    [YamlMember(Alias = "tls")]
    public ClientTLSConfig? TLS { get; set; }
    
    /// <summary>
    /// 日志级别
    /// </summary>
    [YamlMember(Alias = "log_level")]
    public string LogLevel { get; set; } = "info";
}

/// <summary>
/// 客户端 TLS 配置
/// </summary>
public class ClientTLSConfig
{
    /// <summary>
    /// 是否启用 TLS 伪装
    /// </summary>
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; }
    
    /// <summary>
    /// SNI 域名
    /// </summary>
    [YamlMember(Alias = "server_name")]
    public string ServerName { get; set; } = "www.microsoft.com";
    
    /// <summary>
    /// 浏览器指纹
    /// </summary>
    [YamlMember(Alias = "fingerprint")]
    public string Fingerprint { get; set; } = "chrome";
    
    /// <summary>
    /// 是否跳过证书验证
    /// </summary>
    [YamlMember(Alias = "skip_verify")]
    public bool SkipVerify { get; set; } = true;
    
    /// <summary>
    /// ALPN 协议列表
    /// </summary>
    [YamlMember(Alias = "alpn")]
    public List<string>? ALPN { get; set; }
}

/// <summary>
/// TLS 指纹伪装配置 (服务端/通用)
/// </summary>
public class TLSConfig
{
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; }

    [YamlMember(Alias = "server_name")]
    public string ServerName { get; set; } = "www.microsoft.com";

    [YamlMember(Alias = "fingerprint")]
    public string Fingerprint { get; set; } = "chrome";

    [YamlMember(Alias = "enable_ech")]
    public bool EnableECH { get; set; }

    [YamlMember(Alias = "ech_config")]
    public string? ECHConfig { get; set; }

    [YamlMember(Alias = "ech_provider")]
    public string ECHProvider { get; set; } = "cloudflare";

    [YamlMember(Alias = "fallback_enabled")]
    public bool FallbackEnabled { get; set; } = true;

    [YamlMember(Alias = "fallback_addr")]
    public string FallbackAddr { get; set; } = "127.0.0.1:80";

    [YamlMember(Alias = "fallback_timeout")]
    public int FallbackTimeout { get; set; } = 10;

    [YamlMember(Alias = "cert_file")]
    public string? CertFile { get; set; }

    [YamlMember(Alias = "key_file")]
    public string? KeyFile { get; set; }

    [YamlMember(Alias = "auto_cert")]
    public bool AutoCert { get; set; } = true;

    [YamlMember(Alias = "verify_cert")]
    public bool VerifyCert { get; set; }

    [YamlMember(Alias = "alpn")]
    public List<string> ALPN { get; set; } = new() { "h2", "http/1.1" };

    [YamlMember(Alias = "min_version")]
    public string MinVersion { get; set; } = "tls12";

    [YamlMember(Alias = "max_version")]
    public string MaxVersion { get; set; } = "tls13";

    [YamlMember(Alias = "session_ticket")]
    public bool SessionTicket { get; set; } = true;

    [YamlMember(Alias = "random_sni")]
    public bool RandomSNI { get; set; }

    [YamlMember(Alias = "sni_list")]
    public List<string> SNIList { get; set; } = new()
    {
        "www.microsoft.com",
        "www.bing.com",
        "www.apple.com",
        "www.cloudflare.com"
    };

    [YamlMember(Alias = "padding_enabled")]
    public bool PaddingEnabled { get; set; }

    [YamlMember(Alias = "padding_min_size")]
    public int PaddingMinSize { get; set; } = 16;

    [YamlMember(Alias = "padding_max_size")]
    public int PaddingMaxSize { get; set; } = 256;

    [YamlMember(Alias = "fragment_enabled")]
    public bool FragmentEnabled { get; set; }

    [YamlMember(Alias = "fragment_size")]
    public int FragmentSize { get; set; } = 40;

    [YamlMember(Alias = "fragment_sleep_ms")]
    public int FragmentSleepMs { get; set; } = 10;

    [YamlMember(Alias = "mimic_browser_order")]
    public bool MimicBrowserOrder { get; set; } = true;
}

/// <summary>
/// ARQ 配置
/// </summary>
public class ARQConfig
{
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; } = true;

    [YamlMember(Alias = "window_size")]
    public int WindowSize { get; set; } = 256;

    [YamlMember(Alias = "max_retries")]
    public int MaxRetries { get; set; } = 10;

    [YamlMember(Alias = "rto_min_ms")]
    public int RTOMinMs { get; set; } = 100;

    [YamlMember(Alias = "rto_max_ms")]
    public int RTOMaxMs { get; set; } = 10000;

    [YamlMember(Alias = "enable_sack")]
    public bool EnableSACK { get; set; } = true;

    [YamlMember(Alias = "enable_timestamp")]
    public bool EnableTimestamp { get; set; } = true;
}

/// <summary>
/// Hysteria2 拥塞控制配置
/// </summary>
public class Hysteria2Config
{
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; } = true;

    [YamlMember(Alias = "up_mbps")]
    public int UpMbps { get; set; } = 100;

    [YamlMember(Alias = "down_mbps")]
    public int DownMbps { get; set; } = 100;

    [YamlMember(Alias = "disable_mtu")]
    public bool DisableMTU { get; set; }

    [YamlMember(Alias = "initial_window")]
    public int InitialWindow { get; set; } = 32;

    [YamlMember(Alias = "max_window")]
    public int MaxWindow { get; set; } = 512;

    [YamlMember(Alias = "min_rtt_ms")]
    public int MinRTT { get; set; } = 20;

    [YamlMember(Alias = "max_rtt_ms")]
    public int MaxRTT { get; set; } = 500;

    [YamlMember(Alias = "loss_threshold")]
    public double LossThreshold { get; set; } = 0.1;
}

/// <summary>
/// FakeTCP 配置
/// </summary>
public class FakeTCPConfig
{
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; }

    [YamlMember(Alias = "listen")]
    public string Listen { get; set; } = ":54322";

    [YamlMember(Alias = "interface")]
    public string? Interface { get; set; }

    [YamlMember(Alias = "sequence_id")]
    public uint SequenceId { get; set; }

    [YamlMember(Alias = "use_ebpf")]
    public bool UseEBPF { get; set; }
}

/// <summary>
/// WebSocket 配置
/// </summary>
public class WebSocketConfig
{
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; }

    [YamlMember(Alias = "listen")]
    public string Listen { get; set; } = ":54323";

    [YamlMember(Alias = "path")]
    public string Path { get; set; } = "/ws";

    [YamlMember(Alias = "host")]
    public string? Host { get; set; }

    [YamlMember(Alias = "tls")]
    public bool TLS { get; set; }

    [YamlMember(Alias = "cert_file")]
    public string? CertFile { get; set; }

    [YamlMember(Alias = "key_file")]
    public string? KeyFile { get; set; }

    [YamlMember(Alias = "cdn")]
    public bool CDN { get; set; }
}

/// <summary>
/// eBPF 配置
/// </summary>
public class EBPFConfig
{
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; }

    [YamlMember(Alias = "interface")]
    public string? Interface { get; set; }

    [YamlMember(Alias = "xdp_mode")]
    public string XDPMode { get; set; } = "generic";

    [YamlMember(Alias = "program_path")]
    public string? ProgramPath { get; set; }

    [YamlMember(Alias = "map_size")]
    public int MapSize { get; set; } = 65536;

    [YamlMember(Alias = "enable_stats")]
    public bool EnableStats { get; set; }

    [YamlMember(Alias = "enable_tc")]
    public bool EnableTC { get; set; }

    [YamlMember(Alias = "tc_faketcp")]
    public bool TCFakeTCP { get; set; }

    [YamlMember(Alias = "disable_listen")]
    public bool DisableListen { get; set; }
}

/// <summary>
/// 切换器配置
/// </summary>
public class SwitcherConfig
{
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; } = true;

    [YamlMember(Alias = "check_interval_ms")]
    public int CheckInterval { get; set; } = 1000;

    [YamlMember(Alias = "fail_threshold")]
    public int FailThreshold { get; set; } = 3;

    [YamlMember(Alias = "recover_threshold")]
    public int RecoverThreshold { get; set; } = 5;

    [YamlMember(Alias = "rtt_threshold_ms")]
    public int RTTThreshold { get; set; } = 300;

    [YamlMember(Alias = "loss_threshold")]
    public double LossThreshold { get; set; } = 0.3;

    [YamlMember(Alias = "priority")]
    public List<string> Priority { get; set; } = new() { "ebpf", "faketcp", "udp", "websocket" };
}

/// <summary>
/// Metrics 监控配置
/// </summary>
public class MetricsConfig
{
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; } = true;

    [YamlMember(Alias = "listen")]
    public string Listen { get; set; } = ":9100";

    [YamlMember(Alias = "path")]
    public string Path { get; set; } = "/metrics";

    [YamlMember(Alias = "health_path")]
    public string HealthPath { get; set; } = "/health";

    [YamlMember(Alias = "enable_pprof")]
    public bool EnablePprof { get; set; }
}

/// <summary>
/// 隧道配置
/// </summary>
public class TunnelConfig
{
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; }

    [YamlMember(Alias = "mode")]
    public string Mode { get; set; } = "temp";

    [YamlMember(Alias = "domain_mode")]
    public string DomainMode { get; set; } = "auto";

    [YamlMember(Alias = "domain")]
    public string? Domain { get; set; }

    [YamlMember(Alias = "cert_mode")]
    public string CertMode { get; set; } = "auto";

    [YamlMember(Alias = "cf_token")]
    public string? CFToken { get; set; }

    [YamlMember(Alias = "local_addr")]
    public string LocalAddr { get; set; } = "127.0.0.1";

    [YamlMember(Alias = "local_port")]
    public int LocalPort { get; set; }

    [YamlMember(Alias = "protocol")]
    public string Protocol { get; set; } = "http";

    [YamlMember(Alias = "acme_email")]
    public string? ACMEEmail { get; set; }

    [YamlMember(Alias = "acme_provider")]
    public string ACMEProvider { get; set; } = "letsencrypt";

    [YamlMember(Alias = "duckdns_token")]
    public string? DuckDNSToken { get; set; }

    [YamlMember(Alias = "duckdns_domain")]
    public string? DuckDNSDomain { get; set; }
}



