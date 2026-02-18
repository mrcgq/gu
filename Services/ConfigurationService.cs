// =============================================================================
// 文件: Services/ConfigurationService.cs
// 描述: 配置管理服务 - 生成符合内核规范的 YAML 配置
//       修复：显式写入客户端专用字段，确保与 Go 端完美对接
// =============================================================================
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using PhantomGUI.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PhantomGUI.Services;

public class ConfigurationService
{
    private readonly string _appDataPath;
    private readonly string _configPath;
    private readonly string _settingsPath;
    
    private readonly ISerializer _yamlSerializer;
    private readonly IDeserializer _yamlDeserializer;
    
    public AppSettings Settings { get; private set; }
    
    public ConfigurationService()
    {
        _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PhantomGUI"
        );
        Directory.CreateDirectory(_appDataPath);
        
        _configPath = Path.Combine(_appDataPath, "config.yaml");
        _settingsPath = Path.Combine(_appDataPath, "settings.json");
        
        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();
            
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
            
        Settings = LoadSettings();
    }
    
    /// <summary>
    /// 加载应用设置
    /// </summary>
    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载设置失败: {ex.Message}");
        }
        return new AppSettings();
    }
    
    /// <summary>
    /// 保存应用设置
    /// </summary>
    public void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存设置失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 根据服务器配置生成客户端 YAML 配置文件
    /// 修复：显式写入所有客户端必需字段
    /// </summary>
    public string GenerateClientConfigYaml(ServerProfile profile)
    {
        // 构建本地监听地址
        string socksAddr = $"127.0.0.1:{profile.LocalSocksPort}";
        string? httpAddr = profile.LocalHttpPort > 0 
            ? $"127.0.0.1:{profile.LocalHttpPort}" 
            : null;
        
        var config = new PhantomConfig
        {
            // =================================================================
            // 客户端核心配置 (根级字段，兼容性最好)
            // =================================================================
            
            // 服务器连接信息
            ServerAddr = profile.ServerAddress,
            ServerPort = profile.ServerPort,
            
            // 本地代理监听
            SocksAddr = socksAddr,
            HttpAddr = httpAddr,
            
            // 认证信息
            PSK = profile.PSK,
            TimeWindow = profile.TimeWindow,
            
            // 传输模式
            Mode = profile.TransportMode,
            TransportMode = profile.TransportMode,
            
            // 日志级别
            LogLevel = Settings.LogLevel,
            
            // =================================================================
            // 嵌套的客户端配置 (结构化方式，推荐)
            // =================================================================
            Client = new ClientConfig
            {
                ServerAddr = profile.ServerAddress,
                ServerPort = profile.ServerPort,
                PSK = profile.PSK,
                TimeWindow = profile.TimeWindow,
                SocksAddr = socksAddr,
                HttpAddr = httpAddr,
                TransportMode = profile.TransportMode,
                UpMbps = profile.UploadMbps,
                DownMbps = profile.DownloadMbps,
                EnableARQ = profile.EnableARQ,
                LogLevel = Settings.LogLevel,
                
                // TLS 配置
                TLS = profile.TLSEnabled ? new ClientTLSConfig
                {
                    Enabled = true,
                    ServerName = profile.TLSServerName,
                    Fingerprint = profile.TLSFingerprint,
                    SkipVerify = true,
                    ALPN = new System.Collections.Generic.List<string> { "h2", "http/1.1" }
                } : null
            },
            
            // =================================================================
            // Hysteria2 拥塞控制
            // =================================================================
            Hysteria2 = new Hysteria2Config
            {
                Enabled = true,
                UpMbps = profile.UploadMbps,
                DownMbps = profile.DownloadMbps,
                InitialWindow = 32,
                MaxWindow = 512
            },
            
            // =================================================================
            // ARQ 可靠传输
            // =================================================================
            ARQ = new ARQConfig
            {
                Enabled = profile.EnableARQ,
                WindowSize = 256,
                MaxRetries = 10,
                EnableSACK = true,
                EnableTimestamp = true
            },
            
            // =================================================================
            // TLS 伪装配置 (根级，某些 Go 实现可能从这里读取)
            // =================================================================
            TLS = profile.TLSEnabled ? new TLSConfig
            {
                Enabled = true,
                ServerName = profile.TLSServerName,
                Fingerprint = profile.TLSFingerprint,
                VerifyCert = false,
                ALPN = new System.Collections.Generic.List<string> { "h2", "http/1.1" },
                MinVersion = "tls12",
                MaxVersion = "tls13"
            } : null,
            
            // =================================================================
            // Metrics (客户端也可以启用)
            // =================================================================
            Metrics = new MetricsConfig
            {
                Enabled = true,
                Listen = ":9100",
                Path = "/metrics",
                HealthPath = "/health"
            }
        };
        
        return _yamlSerializer.Serialize(config);
    }
    
    /// <summary>
    /// 生成服务端配置（用于测试或服务端部署）
    /// </summary>
    public string GenerateServerConfigYaml(ServerProfile profile)
    {
        int listenPort = profile.LocalSocksPort + 1000;
        
        var config = new PhantomConfig
        {
            Listen = $":{listenPort}",
            PSK = profile.PSK,
            TimeWindow = profile.TimeWindow,
            LogLevel = Settings.LogLevel,
            Mode = profile.TransportMode,
            
            Hysteria2 = new Hysteria2Config
            {
                Enabled = true,
                UpMbps = profile.UploadMbps,
                DownMbps = profile.DownloadMbps
            },
            
            ARQ = new ARQConfig
            {
                Enabled = profile.EnableARQ
            },
            
            TLS = profile.TLSEnabled ? new TLSConfig
            {
                Enabled = true,
                ServerName = profile.TLSServerName,
                Fingerprint = profile.TLSFingerprint
            } : null,
            
            Metrics = new MetricsConfig
            {
                Enabled = true,
                Listen = ":9100"
            }
        };
        
        return _yamlSerializer.Serialize(config);
    }
    
    /// <summary>
    /// 保存客户端配置到文件
    /// </summary>
    public string SaveConfigFile(ServerProfile profile)
    {
        var yaml = GenerateClientConfigYaml(profile);
        var configPath = Path.Combine(_appDataPath, $"config_{profile.Id}.yaml");
        File.WriteAllText(configPath, yaml);
        return configPath;
    }
    
    /// <summary>
    /// 获取生成的配置内容预览
    /// </summary>
    public string GetConfigPreview(ServerProfile profile)
    {
        return GenerateClientConfigYaml(profile);
    }
    
    /// <summary>
    /// 获取内核程序路径
    /// </summary>
    public string GetCorePath()
    {
        // 优先查找程序目录
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        
        // 尝试多个可能的文件名
        var possibleNames = new[]
        {
            "phantom-client.exe",
            "phantom-core.exe",
            "phantom.exe"
        };
        
        foreach (var name in possibleNames)
        {
            var path = Path.Combine(exeDir, name);
            if (File.Exists(path))
                return path;
        }
        
        // 检查 AppData 目录
        foreach (var name in possibleNames)
        {
            var path = Path.Combine(_appDataPath, name);
            if (File.Exists(path))
                return path;
        }
        
        return "";
    }
    
    /// <summary>
    /// 获取日志目录
    /// </summary>
    public string GetLogDirectory()
    {
        var logDir = Path.Combine(_appDataPath, "logs");
        Directory.CreateDirectory(logDir);
        return logDir;
    }
    
    /// <summary>
    /// 获取配置文件目录
    /// </summary>
    public string GetConfigDirectory()
    {
        var configDir = Path.Combine(_appDataPath, "configs");
        Directory.CreateDirectory(configDir);
        return configDir;
    }
    
    /// <summary>
    /// 生成新的 PSK (对应 internal/crypto/crypto.go 的 GeneratePSK)
    /// </summary>
    public static string GeneratePSK()
    {
        var psk = new byte[32]; // PSKSize = 32
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(psk);
        return Convert.ToBase64String(psk);
    }
    
    /// <summary>
    /// 验证 PSK 格式
    /// </summary>
    public static bool ValidatePSK(string psk)
    {
        if (string.IsNullOrWhiteSpace(psk))
            return false;
            
        try
        {
            var bytes = Convert.FromBase64String(psk);
            return bytes.Length == 32;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// 计算 UserID (对应 internal/crypto/crypto.go 的派生逻辑)
    /// </summary>
    public static string CalculateUserId(string psk)
    {
        if (string.IsNullOrWhiteSpace(psk))
            return "invalid";
            
        try
        {
            var pskBytes = Convert.FromBase64String(psk);
            using var hmac = new HMACSHA256(pskBytes);
            var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes("phantom-userid-v3"));
            return BitConverter.ToString(hash, 0, 4).Replace("-", "").ToLower();
        }
        catch
        {
            return "invalid";
        }
    }
    
    /// <summary>
    /// 导出配置到指定路径
    /// </summary>
    public void ExportConfig(ServerProfile profile, string exportPath)
    {
        var yaml = GenerateClientConfigYaml(profile);
        File.WriteAllText(exportPath, yaml);
    }
    
    /// <summary>
    /// 导入配置文件
    /// </summary>
    public ServerProfile? ImportConfig(string configPath)
    {
        try
        {
            var yaml = File.ReadAllText(configPath);
            var config = _yamlDeserializer.Deserialize<PhantomConfig>(yaml);
            
            if (config == null)
                return null;
            
            // 优先从 Client 节点读取
            if (config.Client != null)
            {
                return new ServerProfile
                {
                    Name = Path.GetFileNameWithoutExtension(configPath),
                    ServerAddress = config.Client.ServerAddr,
                    ServerPort = config.Client.ServerPort,
                    PSK = config.Client.PSK,
                    TimeWindow = config.Client.TimeWindow,
                    TransportMode = config.Client.TransportMode,
                    UploadMbps = config.Client.UpMbps,
                    DownloadMbps = config.Client.DownMbps,
                    EnableARQ = config.Client.EnableARQ,
                    LocalSocksPort = ParsePort(config.Client.SocksAddr) ?? 1080,
                    LocalHttpPort = ParsePort(config.Client.HttpAddr) ?? 1081,
                    TLSEnabled = config.Client.TLS?.Enabled ?? false,
                    TLSServerName = config.Client.TLS?.ServerName ?? "www.microsoft.com",
                    TLSFingerprint = config.Client.TLS?.Fingerprint ?? "chrome"
                };
            }
            
            // 回退到根级字段
            return new ServerProfile
            {
                Name = Path.GetFileNameWithoutExtension(configPath),
                ServerAddress = config.ServerAddr ?? "",
                ServerPort = config.ServerPort,
                PSK = config.PSK,
                TimeWindow = config.TimeWindow,
                TransportMode = config.Mode,
                LocalSocksPort = ParsePort(config.SocksAddr) ?? 1080
            };
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// 从地址字符串解析端口号
    /// </summary>
    private static int? ParsePort(string? addr)
    {
        if (string.IsNullOrEmpty(addr))
            return null;
            
        var colonIndex = addr.LastIndexOf(':');
        if (colonIndex >= 0 && colonIndex < addr.Length - 1)
        {
            if (int.TryParse(addr.Substring(colonIndex + 1), out int port))
                return port;
        }
        
        return null;
    }
}


