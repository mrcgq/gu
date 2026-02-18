// =============================================================================
// 文件: Controls/SpeedGauge.xaml.cs
// 描述: 简化版速度显示控件 (不使用图表库)
// =============================================================================
using System;
using System.Windows.Controls;

namespace PhantomGUI.Controls;

public partial class SpeedGauge : UserControl
{
    private double _maxSpeed = 1_000_000; // 初始最大速度 1 MB/s
    
    public SpeedGauge()
    {
        InitializeComponent();
    }
    
    public void AddDataPoint(double upload, double download)
    {
        // 动态调整最大值
        var maxCurrent = Math.Max(upload, download);
        if (maxCurrent > _maxSpeed)
        {
            _maxSpeed = maxCurrent * 1.2;
        }
        else if (maxCurrent < _maxSpeed * 0.3 && _maxSpeed > 1_000_000)
        {
            _maxSpeed = Math.Max(1_000_000, maxCurrent * 2);
        }
        
        // 更新上传
        UploadBar.Maximum = _maxSpeed;
        UploadBar.Value = upload;
        UploadText.Text = $"上传: {FormatSpeed(upload)}";
        
        // 更新下载
        DownloadBar.Maximum = _maxSpeed;
        DownloadBar.Value = download;
        DownloadText.Text = $"下载: {FormatSpeed(download)}";
    }
    
    private static string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond >= 1_000_000_000)
            return $"{bytesPerSecond / 1_000_000_000:F2} GB/s";
        if (bytesPerSecond >= 1_000_000)
            return $"{bytesPerSecond / 1_000_000:F2} MB/s";
        if (bytesPerSecond >= 1_000)
            return $"{bytesPerSecond / 1_000:F2} KB/s";
        return $"{bytesPerSecond:F0} B/s";
    }
}
