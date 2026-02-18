// =============================================================================
// 文件: MainWindow.xaml.cs
// 描述: 主窗口代码后台
// =============================================================================
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using PhantomGUI.ViewModels;

namespace PhantomGUI;

public partial class MainWindow : Window
{
    private TaskbarIcon? _notifyIcon;
    private MainViewModel? _viewModel;
    
    public MainWindow()
    {
        InitializeComponent();
        
        _viewModel = DataContext as MainViewModel;
        
        InitializeNotifyIcon();
        
        Closing += OnWindowClosing;
        StateChanged += OnStateChanged;
    }
    
    private void InitializeNotifyIcon()
    {
        _notifyIcon = new TaskbarIcon
        {
            Icon = new System.Drawing.Icon("Resources/Icons/phantom.ico"),
            ToolTipText = "Phantom VPN",
            Visibility = Visibility.Collapsed
        };
        
        _notifyIcon.TrayMouseDoubleClick += (s, e) =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        };
        
        // 右键菜单
        var contextMenu = new ContextMenu();
        
        var showItem = new MenuItem { Header = "显示窗口" };
        showItem.Click += (s, e) =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        };
        
        var exitItem = new MenuItem { Header = "退出" };
        exitItem.Click += (s, e) =>
        {
            _notifyIcon.Dispose();
            Application.Current.Shutdown();
        };
        
        contextMenu.Items.Add(showItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(exitItem);
        
        _notifyIcon.ContextMenu = contextMenu;
    }
    
    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            // 最小化到托盘
            Hide();
            _notifyIcon!.Visibility = Visibility.Visible;
            _notifyIcon.ShowBalloonTip("Phantom VPN", "程序已最小化到系统托盘", BalloonIcon.Info);
        }
        else
        {
            _notifyIcon!.Visibility = Visibility.Collapsed;
        }
    }
    
    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        // 询问是否退出
        var result = MessageBox.Show(
            "确定要退出吗？退出后VPN连接将断开。",
            "确认退出",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
            
        if (result == MessageBoxResult.No)
        {
            e.Cancel = true;
            return;
        }
        
        // 清理资源
        _viewModel?.Dispose();
        _notifyIcon?.Dispose();
    }
}




