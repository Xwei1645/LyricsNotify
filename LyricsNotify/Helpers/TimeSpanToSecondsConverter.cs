using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LyricsNotify.Helpers;

public class TimeSpanToSecondsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan ts)
        {
            return ts.TotalSeconds;
        }
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return TimeSpan.FromSeconds(d);
        }
        if (value is float f)
        {
            return TimeSpan.FromSeconds(f);
        }
        if (double.TryParse(value?.ToString(), out var res))
        {
            return TimeSpan.FromSeconds(res);
        }
        return TimeSpan.Zero;
    }
}
