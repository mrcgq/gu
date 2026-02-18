// =============================================================================
// 文件: Controls/SpeedGauge.xaml.cs
// 描述: 速度仪表盘控件代码
// =============================================================================
using System;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Configurations;

namespace PhantomGUI.Controls;

public partial class SpeedGauge : UserControl
{
    private const int MaxPoints = 60; // 保留60个数据点
    
    public ChartValues<double> UploadHistory { get; set; }
    public ChartValues<double> DownloadHistory { get; set; }
    
    public Func<double, string> YFormatter { get; set; }
    
    public SpeedGauge()
    {
        InitializeComponent();
        DataContext = this;
        
        UploadHistory = new ChartValues<double>();
        DownloadHistory = new ChartValues<double>();
        
        // 初始化数据点
        for (int i = 0; i < MaxPoints; i++)
        {
            UploadHistory.Add(0);
            DownloadHistory.Add(0);
        }
        
        YFormatter = value => FormatSpeed(value);
    }
    
    public void AddDataPoint(double upload, double download)
    {
        UploadHistory.Add(upload);
        DownloadHistory.Add(download);
        
        if (UploadHistory.Count > MaxPoints)
            UploadHistory.RemoveAt(0);
        if (DownloadHistory.Count > MaxPoints)
            DownloadHistory.RemoveAt(0);
    }
    
    private static string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond >= 1_000_000)
            return $"{bytesPerSecond / 1_000_000:F1} MB/s";
        if (bytesPerSecond >= 1_000)
            return $"{bytesPerSecond / 1_000:F1} KB/s";
        return $"{bytesPerSecond:F0} B/s";
    }
}

