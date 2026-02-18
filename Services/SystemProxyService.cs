// =============================================================================
// 文件: Services/SystemProxyService.cs
// 描述: Windows 系统代理设置
// =============================================================================
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace PhantomGUI.Services;

public class SystemProxyService
{
    private const string REGISTRY_KEY = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
    
    [DllImport("wininet.dll")]
    private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
    
    private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
    private const int INTERNET_OPTION_REFRESH = 37;
    
    /// <summary>
    /// 设置系统 SOCKS5 代理
    /// </summary>
    public static void SetSocksProxy(string host, int port)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true);
            if (key == null) return;
            
            key.SetValue("ProxyEnable", 1);
            key.SetValue("ProxyServer", $"socks={host}:{port}");
            
            RefreshSystemProxy();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"设置代理失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 设置系统 HTTP 代理
    /// </summary>
    public static void SetHttpProxy(string host, int port)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true);
            if (key == null) return;
            
            key.SetValue("ProxyEnable", 1);
            key.SetValue("ProxyServer", $"{host}:{port}");
            
            RefreshSystemProxy();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"设置代理失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 清除系统代理
    /// </summary>
    public static void ClearProxy()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true);
            if (key == null) return;
            
            key.SetValue("ProxyEnable", 0);
            key.DeleteValue("ProxyServer", false);
            
            RefreshSystemProxy();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"清除代理失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 获取当前代理状态
    /// </summary>
    public static (bool enabled, string? server) GetCurrentProxy()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, false);
            if (key == null) return (false, null);
            
            var enabled = (int)(key.GetValue("ProxyEnable") ?? 0) == 1;
            var server = key.GetValue("ProxyServer") as string;
            
            return (enabled, server);
        }
        catch
        {
            return (false, null);
        }
    }
    
    /// <summary>
    /// 刷新系统代理设置
    /// </summary>
    private static void RefreshSystemProxy()
    {
        InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
        InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
    }
}



