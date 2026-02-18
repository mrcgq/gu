// =============================================================================
// 文件: Converters/Converters.cs
// 描述: WPF 值转换器集合 (完整版，含所有转换器)
// =============================================================================
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using PhantomGUI.Services;

namespace PhantomGUI.Converters;

/// <summary>
/// 布尔值转可见性
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility v)
        {
            return v == Visibility.Visible;
        }
        return false;
    }
}

/// <summary>
/// 反向布尔值转可见性
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility v)
        {
            return v != Visibility.Visible;
        }
        return true;
    }
}

/// <summary>
/// 字符串转可见性 (非空字符串 = Visible)
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrWhiteSpace(s))
        {
            return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 连接状态转连接按钮可见性
/// </summary>
public class ConnectionStateToConnectVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ConnectionState state)
        {
            return state == ConnectionState.Disconnected || state == ConnectionState.Error
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 连接状态转断开按钮可见性
/// </summary>
public class ConnectionStateToDisconnectVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ConnectionState state)
        {
            return state == ConnectionState.Connected || state == ConnectionState.Connecting
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 日志级别提取器
/// </summary>
public class LogLevelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string log)
        {
            // 错误级别
            if (log.Contains("[ERROR]") || 
                log.Contains("错误") || 
                log.Contains("失败") ||
                log.Contains("✗"))
            {
                return "ERROR";
            }
            
            // 警告级别
            if (log.Contains("[WARN]") || 
                log.Contains("警告") || 
                log.Contains("⚠️") ||
                log.Contains("⚠"))
            {
                return "WARN";
            }
            
            // 成功级别
            if (log.Contains("✓") || 
                log.Contains("成功") ||
                log.Contains("已连接") ||
                log.Contains("已启动"))
            {
                return "SUCCESS";
            }
            
            // 调试级别
            if (log.Contains("[DEBUG]"))
            {
                return "DEBUG";
            }
        }
        return "INFO";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值转图标 (格式: "TrueIcon|FalseIcon")
/// </summary>
public class BoolToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && parameter is string icons)
        {
            var parts = icons.Split('|');
            if (parts.Length == 2)
            {
                return b ? parts[0] : parts[1];
            }
        }
        return "Help";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值转颜色 (格式: "TrueColor|FalseColor")
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && parameter is string colors)
        {
            var parts = colors.Split('|');
            if (parts.Length == 2)
            {
                var colorStr = b ? parts[0] : parts[1];
                
                // 如果需要返回 Brush
                if (targetType == typeof(Brush) || targetType == typeof(SolidColorBrush))
                {
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString(colorStr);
                        return new SolidColorBrush(color);
                    }
                    catch
                    {
                        return new SolidColorBrush(Colors.Gray);
                    }
                }
                
                return colorStr;
            }
        }
        return "#808080";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值转 Brush
/// </summary>
public class BoolToBrushConverter : IValueConverter
{
    public Brush TrueBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)); // 绿色
    public Brush FalseBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00)); // 橙色
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? TrueBrush : FalseBrush;
        }
        return FalseBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 字节数格式化
/// </summary>
public class BytesFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        long bytes = 0;
        
        if (value is long l)
            bytes = l;
        else if (value is int i)
            bytes = i;
        else if (value is double d)
            bytes = (long)d;
        else if (value is uint ui)
            bytes = ui;
        else if (value is ulong ul)
            bytes = (long)ul;
            
        if (bytes >= 1_000_000_000_000)
            return $"{bytes / 1_000_000_000_000.0:F2} TB";
        if (bytes >= 1_000_000_000)
            return $"{bytes / 1_000_000_000.0:F2} GB";
        if (bytes >= 1_000_000)
            return $"{bytes / 1_000_000.0:F2} MB";
        if (bytes >= 1_000)
            return $"{bytes / 1_000.0:F2} KB";
        return $"{bytes} B";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 速度格式化 (bytes/second)
/// </summary>
public class SpeedFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double bytesPerSecond = 0;
        
        if (value is double d)
            bytesPerSecond = d;
        else if (value is long l)
            bytesPerSecond = l;
        else if (value is int i)
            bytesPerSecond = i;
            
        if (bytesPerSecond >= 1_000_000_000)
            return $"{bytesPerSecond / 1_000_000_000:F2} GB/s";
        if (bytesPerSecond >= 1_000_000)
            return $"{bytesPerSecond / 1_000_000:F2} MB/s";
        if (bytesPerSecond >= 1_000)
            return $"{bytesPerSecond / 1_000:F2} KB/s";
        return $"{bytesPerSecond:F0} B/s";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 状态颜色转换器
/// </summary>
public class StateColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string colorHex = "#808080"; // 默认灰色
        
        if (value is string state)
        {
            colorHex = state.ToLower() switch
            {
                "running" or "connected" or "healthy" => "#4CAF50",   // 绿色
                "connecting" or "switching" or "degraded" => "#FF9800", // 橙色
                "failed" or "error" or "disconnected" => "#F44336",   // 红色
                "idle" or "stopped" => "#9E9E9E",                     // 灰色
                _ => "#808080"
            };
        }
        
        // 如果需要返回 Brush
        if (targetType == typeof(Brush) || targetType == typeof(SolidColorBrush))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorHex);
                return new SolidColorBrush(color);
            }
            catch
            {
                return new SolidColorBrush(Colors.Gray);
            }
        }
        
        return colorHex;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 连接状态转颜色
/// </summary>
public class ConnectionStateToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string colorHex = "#808080";
        
        if (value is ConnectionState state)
        {
            colorHex = state switch
            {
                ConnectionState.Connected => "#4CAF50",
                ConnectionState.Connecting or ConnectionState.Disconnecting => "#FF9800",
                ConnectionState.Error => "#F44336",
                ConnectionState.Disconnected => "#808080",
                _ => "#808080"
            };
        }
        
        if (targetType == typeof(Brush) || targetType == typeof(SolidColorBrush))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorHex);
                return new SolidColorBrush(color);
            }
            catch
            {
                return new SolidColorBrush(Colors.Gray);
            }
        }
        
        return colorHex;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 传输模式转图标
/// </summary>
public class TransportModeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string mode)
        {
            return mode.ToLower() switch
            {
                "udp" => "AccessPointNetwork",
                "faketcp" => "ShieldHalfFull",
                "websocket" => "Web",
                "ebpf" => "Chip",
                "auto" => "AutoFix",
                _ => "HelpCircle"
            };
        }
        return "HelpCircle";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 空值转可见性
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool invert = parameter?.ToString()?.ToLower() == "invert";
        bool isNull = value == null;
        
        if (invert)
        {
            return isNull ? Visibility.Visible : Visibility.Collapsed;
        }
        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 时间跨度格式化
/// </summary>
public class TimeSpanFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSpan ts)
        {
            if (ts.TotalDays >= 1)
                return $"{(int)ts.TotalDays}天 {ts.Hours}小时";
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}小时 {ts.Minutes}分钟";
            if (ts.TotalMinutes >= 1)
                return $"{(int)ts.TotalMinutes}分钟 {ts.Seconds}秒";
            return $"{ts.Seconds}秒";
        }
        return "-";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 百分比格式化 (0-1 转 0%-100%)
/// </summary>
public class PercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return $"{d * 100:F1}%";
        }
        if (value is float f)
        {
            return $"{f * 100:F1}%";
        }
        return "0%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 多值转换器：所有值都为 true 时返回 Visible
/// </summary>
public class MultiBoolToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        foreach (var value in values)
        {
            if (value is bool b && !b)
                return Visibility.Collapsed;
            if (value == null)
                return Visibility.Collapsed;
        }
        return Visibility.Visible;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}







