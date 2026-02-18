// =============================================================================
// 文件: Controls/SpeedGauge.xaml.cs
// 描述: 速度仪表盘控件代码 (使用 LiveCharts2)
// =============================================================================
using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace PhantomGUI.Controls;

public partial class SpeedGauge : UserControl
{
    private const int MaxPoints = 60;
    
    private readonly ObservableCollection<double> _uploadValues = new();
    private readonly ObservableCollection<double> _downloadValues = new();
    
    public ISeries[] Series { get; set; }
    public Axis[] XAxes { get; set; }
    public Axis[] YAxes { get; set; }
    
    public SpeedGauge()
    {
        InitializeComponent();
        DataContext = this;
        
        // 初始化数据点
        for (int i = 0; i < MaxPoints; i++)
        {
            _uploadValues.Add(0);
            _downloadValues.Add(0);
        }
        
        // 配置图表系列
        Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Name = "上传",
                Values = _uploadValues,
                Stroke = new SolidColorPaint(SKColors.LimeGreen) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 0,
                LineSmoothness = 0.5
            },
            new LineSeries<double>
            {
                Name = "下载",
                Values = _downloadValues,
                Stroke = new SolidColorPaint(SKColors.DodgerBlue) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 0,
                LineSmoothness = 0.5
            }
        };
        
        // 配置 X 轴
        XAxes = new Axis[]
        {
            new Axis
            {
                ShowSeparatorLines = false,
                Labels = null,
                IsVisible = false
            }
        };
        
        // 配置 Y 轴
        YAxes = new Axis[]
        {
            new Axis
            {
                MinLimit = 0,
                Labeler = value => FormatSpeed(value),
                LabelsPaint = new SolidColorPaint(SKColors.Gray)
            }
        };
    }
    
    public void AddDataPoint(double upload, double download)
    {
        _uploadValues.Add(upload);
        _downloadValues.Add(download);
        
        if (_uploadValues.Count > MaxPoints)
            _uploadValues.RemoveAt(0);
        if (_downloadValues.Count > MaxPoints)
            _downloadValues.RemoveAt(0);
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
