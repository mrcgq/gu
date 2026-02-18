// =============================================================================
// 文件: App.xaml.cs
// 描述: 应用程序启动逻辑 (简化版)
// =============================================================================
using System;
using System.Windows;

namespace PhantomGUI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // 全局异常处理
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            MessageBox.Show(
                $"程序发生严重错误：\n\n{ex?.Message}\n\n{ex?.StackTrace}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };
        
        DispatcherUnhandledException += (sender, args) =>
        {
            MessageBox.Show(
                $"程序发生错误：\n\n{args.Exception.Message}\n\n{args.Exception.StackTrace}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };
        
        base.OnStartup(e);
    }
}
