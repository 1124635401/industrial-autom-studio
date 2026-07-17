using System.Globalization;
using System.Windows.Data;

namespace IndustrialAutomationStudio.Modules.Motion.Converters;

public sealed class EqualityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not null && parameter is not null && Equals(value.ToString(), parameter.ToString());

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
