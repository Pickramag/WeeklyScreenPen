using System.Globalization;
using System.Windows.Data;
using Binding = System.Windows.Data.Binding;

namespace WeeklyScreenPen.Core;

public sealed class SizePreviewConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double v = value is double d ? d : 4;
        return Math.Clamp(v * 0.6, 4, 28);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
