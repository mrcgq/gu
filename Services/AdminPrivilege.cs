// =============================================================================
// 文件: Services/AdminPrivilege.cs
// 描述: Windows 管理员权限检测与提权服务
//       对应内核的 FakeTCP/eBPF 模式需要 Raw Socket 权限
// =============================================================================
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;

namespace PhantomGUI.Services;

/// <summary>
/// 管理员权限服务
/// </summary>
public static class AdminPrivilege
{
    /// <summary>
    /// 检查当前是否以管理员身份运行
    /// </summary>
    public static bool IsRunningAsAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// 检查指定模式是否需要管理员权限
    /// 对应 internal/switcher/types.go 中的 TransportMode
    /// </summary>
    public static bool RequiresAdmin(string mode)
    {
        if (string.IsNullOrEmpty(mode))
            return false;
            
        var modeLower = mode.ToLower();
        
        // FakeTCP 需要 Raw Socket
        // eBPF 在 Windows 上实际不支持，但如果尝试使用也需要特殊权限
        return modeLower switch
        {
            "faketcp" => true,
            "ebpf" => true,
            _ => false
        };
    }
    
    /// <summary>
    /// 获取模式所需权限的说明
    /// </summary>
    public static string GetPrivilegeDescription(string mode)
    {
        var modeLower = mode?.ToLower() ?? "";
        
        return modeLower switch
        {
            "faketcp" => "FakeTCP 模式需要创建原始套接字 (Raw Socket)，这在 Windows 下必须以管理员身份运行。",
            "ebpf" => "eBPF 模式需要加载内核程序，这在 Windows 下必须以管理员身份运行。\n注意：eBPF 主要支持 Linux 系统。",
            _ => ""
        };
    }
    
    /// <summary>
    /// 请求以管理员身份重启应用
    /// </summary>
    /// <returns>true 如果成功启动新进程</returns>
    public static bool RestartAsAdmin()
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
                return false;
                
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas", // 触发 UAC 提权
                WorkingDirectory = Environment.CurrentDirectory
            };
            
            // 传递当前的命令行参数
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                startInfo.Arguments = string.Join(" ", args, 1, args.Length - 1);
            }
            
            Process.Start(startInfo);
            return true;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            // 用户取消了 UAC 提示
            if (ex.NativeErrorCode == 1223) // ERROR_CANCELLED
            {
                return false;
            }
            throw;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// 显示权限不足对话框并提供重启选项
    /// </summary>
    /// <param name="mode">当前选择的模式</param>
    /// <returns>true 如果用户选择重启并成功启动</returns>
    public static bool ShowPrivilegeDialog(string mode)
    {
        var description = GetPrivilegeDescription(mode);
        
        var result = MessageBox.Show(
            $"当前选择的 {mode.ToUpper()} 模式需要管理员权限。\n\n" +
            $"{description}\n\n" +
            "是否以管理员身份重新启动程序？\n\n" +
            "点击「是」将关闭当前程序并以管理员身份重启。\n" +
            "点击「否」将切换到不需要特殊权限的 UDP 模式。",
            "需要管理员权限",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
            
        if (result == MessageBoxResult.Yes)
        {
            if (RestartAsAdmin())
            {
                // 关闭当前程序
                Application.Current.Shutdown();
                return true;
            }
            else
            {
                MessageBox.Show(
                    "无法以管理员身份启动程序。\n\n" +
                    "可能的原因：\n" +
                    "• 用户取消了 UAC 提示\n" +
                    "• 系统策略阻止了提权\n\n" +
                    "程序将使用 UDP 模式继续运行。",
                    "提权失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 检查权限并根据需要提示用户
    /// </summary>
    /// <param name="mode">要使用的传输模式</param>
    /// <param name="fallbackMode">权限不足时的回退模式</param>
    /// <returns>实际应该使用的模式</returns>
    public static string CheckAndPrompt(string mode, string fallbackMode = "udp")
    {
        if (!RequiresAdmin(mode))
            return mode;
            
        if (IsRunningAsAdmin())
            return mode;
            
        // 需要权限但没有权限，显示对话框
        var restarted = ShowPrivilegeDialog(mode);
        
        if (restarted)
        {
            // 程序即将重启，返回原模式
            return mode;
        }
        
        // 用户选择不提权，回退到安全模式
        return fallbackMode;
    }
    
    /// <summary>
    /// 获取当前用户权限级别的描述
    /// </summary>
    public static string GetCurrentPrivilegeLevel()
    {
        if (IsRunningAsAdmin())
            return "管理员";
            
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            
            if (principal.IsInRole(WindowsBuiltInRole.User))
                return "标准用户";
            if (principal.IsInRole(WindowsBuiltInRole.Guest))
                return "来宾用户";
                
            return "受限用户";
        }
        catch
        {
            return "未知";
        }
    }
}

