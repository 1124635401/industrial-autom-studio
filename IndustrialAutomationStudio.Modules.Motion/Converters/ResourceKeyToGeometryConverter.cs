using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace IndustrialAutomationStudio.Modules.Motion.Converters;

public sealed class ResourceKeyToGeometryConverter : IValueConverter
{
    private static readonly Geometry Fallback =
        Geometry.Parse("M4,4 L20,4 20,20 4,20 Z");

    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not string key || string.IsNullOrWhiteSpace(key))
        {
            return Fallback;
        }

        return Application.Current?.TryFindResource(key) as Geometry ?? Fallback;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture) =>
        throw new NotSupportedException();
}
