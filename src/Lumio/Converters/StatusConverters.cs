using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Lumio.Core.Model;
using Lumio.Theme.Controls;

namespace Lumio.Converters;

/// <summary>Maps a connection status to the badge style that shows it.</summary>
public sealed class StatusToBadgeKindConverter : IValueConverter
{
    public static readonly StatusToBadgeKindConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        ConnectionStatus.Connected => BadgeKind.Success,
        ConnectionStatus.NeedsAttention => BadgeKind.Warning,
        ConnectionStatus.Failed => BadgeKind.Danger,
        _ => BadgeKind.Neutral,
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        BindingOperations.DoNothing;
}

/// <summary>
/// Two-way bridge between an enum property and a RadioButton's IsChecked.
/// ConverterParameter is the enum member name.
/// </summary>
public sealed class EnumEqualsConverter : IValueConverter
{
    public static readonly EnumEqualsConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Enum e && parameter is string name && string.Equals(e.ToString(), name, StringComparison.Ordinal);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is string name)
        {
            var enumType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (enumType.IsEnum && Enum.TryParse(enumType, name, out var result))
                return result;
        }
        return BindingOperations.DoNothing;
    }
}
