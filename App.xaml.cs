// =============================================================================
// 文件: App.xaml.cs
// 描述: 应用程序启动逻辑
//       修复：增加单实例检测、全局异常处理、启动参数解析
// =============================================================================
using System;
using System.Linq;
using System.Threading;
using System.Windows;
using PhantomGUI.Services;

namespace PhantomGUI;

public partial class App : Application
{
    private static Mutex? _mutex;
    private const string MutexName = "PhantomGUI_SingleInstance_v4";
    
    /// <summary>
    /// 命令行启动参数
    /// </summary>
    public static class StartupArgs
    {
        public static bool AutoConnect { get; set; }
        public static string? ProfileId { get; set; }
        public static bool Minimized { get; set; }
    }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        // 1. 解析命令行参数
        ParseCommandLineArgs(e.Args);
        
        // 2. 单实例检测
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        
        if (!createdNew)
        {
            // 已有实例运行，尝试激活它
            ActivateExistingInstance();
            Shutdown();
            return;
        }
        
        // 3. 设置全局异常处理
        SetupExceptionHandlers();
        
        // 4. 检查运行环境
        if (!CheckEnvironment())
        {
            Shutdown();
            return;
        }
        
        // 5. 显示权限状态
        var privilegeLevel = AdminPrivilege.GetCurrentPrivilegeLevel();
        System.Diagnostics.Debug.WriteLine($"当前权限级别: {privilegeLevel}");
        
        base.OnStartup(e);
    }
    
    /// <summary>
    /// 解析命令行参数
    /// </summary>
    private void ParseCommandLineArgs(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--autoconnect":
                case "-a":
                    StartupArgs.AutoConnect = true;
                    break;
                    
                case "--profile":
                case "-p":
                    if (i + 1 < args.Length)
                    {
                        StartupArgs.ProfileId = args[++i];
                    }
                    break;
                    
                case "--minimized":
                case "-m":
                    StartupArgs.Minimized = true;
                    break;
            }
        }
    }
    
    /// <summary>
    /// 激活已存在的实例
    /// </summary>
    private void ActivateExistingInstance()
    {
        MessageBox.Show(
            "Phantom VPN 已在运行中。\n\n请检查系统托盘图标。",
            "提示",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
    
    /// <summary>
    /// 设置全局异常处理
    /// </summary>
    private void SetupExceptionHandlers()
    {
        // AppDomain 未处理异常
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            LogFatalError("AppDomain", ex);
            
            if (args.IsTerminating)
            {
                MessageBox.Show(
                    $"程序发生严重错误，即将关闭。\n\n错误信息: {ex?.Message}\n\n" +
                    "请查看日志文件获取详细信息。",
                    "严重错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        };
        
        // WPF UI 线程异常
        DispatcherUnhandledException += (sender, args) =>
        {
            LogFatalError("UI Thread", args.Exception);
            
            var result = MessageBox.Show(
                $"程序发生错误:\n\n{args.Exception.Message}\n\n" +
                "是否继续运行？\n\n" +
                "点击「是」忽略此错误继续运行\n" +
                "点击「否」关闭程序",
                "程序错误",
                MessageBoxButton.YesNo,
                MessageBoxImage.Error);
                
            args.Handled = (result == MessageBoxResult.Yes);
        };
        
        // Task 未观察到的异常
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            LogFatalError("Task", args.Exception);
            args.SetObserved(); // 防止程序崩溃
        };
    }
    
    /// <summary>
    /// 记录致命错误
    /// </summary>
    private void LogFatalError(string source, Exception? ex)
    {
        try
        {
            var logPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PhantomGUI",
                "crash.log"
            );
            
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);
            
            var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}] {ex?.ToString()}\n\n";
            System.IO.File.AppendAllText(logPath, message);
        }
        catch
        {
            // 忽略日志写入错误
        }
    }
    
    /// <summary>
    /// 检查运行环境
    /// </summary>
    private bool CheckEnvironment()
    {
        // 检查 .NET 版本
        var version = Environment.Version;
        if (version.Major < 8)
        {
            MessageBox.Show(
                $"此程序需要 .NET 8.0 或更高版本。\n\n" +
                $"当前版本: {version}\n\n" +
                "请从 Microsoft 官网下载安装 .NET 8.0 Runtime。",
                "运行环境错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
        
        // 检查 Windows 版本 (Windows 10 1903+ 或 Windows 11)
        var osVersion = Environment.OSVersion;
        if (osVersion.Version.Major < 10)
        {
            var result = MessageBox.Show(
                "此程序推荐在 Windows 10 或更高版本上运行。\n\n" +
                $"当前系统: {osVersion.VersionString}\n\n" +
                "部分功能可能无法正常工作。是否继续？",
                "系统版本警告",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
                
            if (result == MessageBoxResult.No)
                return false;
        }
        
        return true;
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}

