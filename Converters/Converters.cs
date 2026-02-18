// =============================================================================
// 文件: Converters/Converters.cs
// 描述: WPF 值转换器集合 (简化版)
// =============================================================================
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

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
/// 日志级别提取器
/// </summary>
public class LogLevelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string log)
        {
            if (log.Contains("[ERROR]") || log.Contains("错误") || log.Contains("失败") || log.Contains("✗"))
            {
                return "ERROR";
            }
            if (log.Contains("[WARN]") || log.Contains("警告") || log.Contains("⚠"))
            {
                return "WARN";
            }
            if (log.Contains("✓") || log.Contains("成功") || log.Contains("已连接") || log.Contains("已启动"))
            {
                return "SUCCESS";
            }
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
