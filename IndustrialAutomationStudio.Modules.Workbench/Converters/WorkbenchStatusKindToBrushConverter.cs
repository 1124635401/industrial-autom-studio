using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using IndustrialAutomationStudio.Modules.Workbench.ViewModels;

namespace IndustrialAutomationStudio.Modules.Workbench.Converters;

public sealed class WorkbenchStatusKindToBrushConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not WorkbenchStatusKind kind)
            return null;

        return kind switch
        {
            WorkbenchStatusKind.Normal => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF1677FF")),
            WorkbenchStatusKind.Success => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF20B26B")),
            WorkbenchStatusKind.Warning => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF5A623")),
            WorkbenchStatusKind.Error => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF04444")),
            WorkbenchStatusKind.Info => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF722ED1")),
            _ => null
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public sealed class WorkbenchStatusKindToLightBrushConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not WorkbenchStatusKind kind)
            return null;

        return kind switch
        {
            WorkbenchStatusKind.Normal => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEAF3FF")),
            WorkbenchStatusKind.Success => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFECF9F2")),
            WorkbenchStatusKind.Warning => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFF7E8")),
            WorkbenchStatusKind.Error => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFEEEE")),
            WorkbenchStatusKind.Info => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3E8FF")),
            _ => null
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
