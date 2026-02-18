// =============================================================================
// 文件: MainWindow.xaml.cs
// 描述: 主窗口代码后台 (简化版)
// =============================================================================
using System;
using System.ComponentModel;
using System.Windows;
using PhantomGUI.ViewModels;

namespace PhantomGUI;

public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;
    
    public MainWindow()
    {
        InitializeComponent();
        _viewModel = DataContext as MainViewModel;
        Closing += OnWindowClosing;
    }
    
    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
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
        
        _viewModel?.Dispose();
    }
}
