using System.Globalization;
using System.Windows.Data;

namespace CopyTrail.Converters;

[ValueConversion(typeof(DateTime), typeof(string))]
public sealed class RelativeTimeConverter : IValueConverter
{
    public static string ToRelative(DateTime utcTime)
    {
        var elapsed = DateTime.UtcNow - utcTime;

        if (elapsed.TotalSeconds < 60) return "Just now";
        if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
        if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours}h ago";
        if (elapsed.TotalDays < 7) return $"{(int)elapsed.TotalDays}d ago";

        return utcTime.ToLocalTime().ToString("MMM d", CultureInfo.CurrentCulture);
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt)
            return ToRelative(dt);
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
