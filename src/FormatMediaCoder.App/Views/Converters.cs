using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FormatMediaCoder.App.Views;

/// <summary>Colapsa un elemento cuando el texto enlazado está vacío.</summary>
public sealed class EmptyToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
