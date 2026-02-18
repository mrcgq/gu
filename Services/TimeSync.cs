// =============================================================================
// 文件: Services/TimeSync.cs
// 描述: 时间同步检查 - 对应 internal/crypto/crypto.go 的 TSKD 认证要求
// =============================================================================
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PhantomGUI.Services;

public class TimeSyncService
{
    // TSKD 允许的最大时间偏差（秒）- 对应 internal/crypto/crypto.go 的 timeWindow
    private const int MAX_TIME_OFFSET_SECONDS = 30;
    
    // NTP 服务器列表
    private static readonly string[] NtpServers = new[]
    {
        "time.windows.com",
        "pool.ntp.org",
        "time.google.com",
        "time.cloudflare.com"
    };
    
    /// <summary>
    /// 检查时间同步状态
    /// </summary>
    public static async Task<TimeSyncResult> CheckTimeSyncAsync()
    {
        foreach (var server in NtpServers)
        {
            try
            {
                var ntpTime = await GetNtpTimeAsync(server);
                var localTime = DateTime.UtcNow;
                var offset = (ntpTime - localTime).TotalSeconds;
                
                return new TimeSyncResult
                {
                    Success = true,
                    NtpServer = server,
                    NtpTime = ntpTime,
                    LocalTime = localTime,
                    OffsetSeconds = offset,
                    IsSynced = Math.Abs(offset) <= MAX_TIME_OFFSET_SECONDS
                };
            }
            catch
            {
                continue;
            }
        }
        
        return new TimeSyncResult
        {
            Success = false,
            LocalTime = DateTime.UtcNow,
            IsSynced = true // 无法检测时假设同步
        };
    }
    
    /// <summary>
    /// 从 NTP 服务器获取时间
    /// </summary>
    private static async Task<DateTime> GetNtpTimeAsync(string server)
    {
        var ntpData = new byte[48];
        ntpData[0] = 0x1B; // NTP 请求头
        
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.ReceiveTimeout = 3000;
        
        var addresses = await Dns.GetHostAddressesAsync(server);
        var endpoint = new IPEndPoint(addresses[0], 123);
        
        await socket.ConnectAsync(endpoint);
        await socket.SendAsync(ntpData, SocketFlags.None);
        await socket.ReceiveAsync(ntpData, SocketFlags.None);
        
        // 解析 NTP 时间
        ulong intPart = (ulong)ntpData[40] << 24 | (ulong)ntpData[41] << 16 |
                        (ulong)ntpData[42] << 8 | ntpData[43];
        ulong fractPart = (ulong)ntpData[44] << 24 | (ulong)ntpData[45] << 16 |
                          (ulong)ntpData[46] << 8 | ntpData[47];
        
        var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
        var ntpTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMilliseconds((long)milliseconds);
        
        return ntpTime;
    }
}

public class TimeSyncResult
{
    public bool Success { get; set; }
    public string? NtpServer { get; set; }
    public DateTime NtpTime { get; set; }
    public DateTime LocalTime { get; set; }
    public double OffsetSeconds { get; set; }
    public bool IsSynced { get; set; }
    
    public string Message
    {
        get
        {
            if (!Success)
                return "无法连接到时间服务器";
            if (IsSynced)
                return $"时间同步正常 (偏差: {OffsetSeconds:F1}秒)";
            return $"⚠️ 时间偏差过大: {OffsetSeconds:F1}秒，TSKD 认证可能失败！";
        }
    }
}


