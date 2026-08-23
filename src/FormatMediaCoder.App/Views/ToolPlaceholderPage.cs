using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace FormatMediaCoder.App.Views;

/// <summary>
/// Sitio de cada herramienta aún no portada, dentro del nuevo menú (brief §6.2).
/// El porte de su pantalla es trabajo aparte; el flujo "Preparar Evento" es la prioridad.
/// </summary>
public sealed class ToolPlaceholderPage : Page
{
    public ToolPlaceholderPage(string toolName)
    {
        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = 460,
            Spacing = 10,
        };

        panel.Children.Add(new FontIcon
        {
            Glyph = "",
            FontSize = 30,
            Foreground = (Brush)Application.Current.Resources["FmcText3"],
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        panel.Children.Add(new TextBlock
        {
            Text = toolName,
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        panel.Children.Add(new TextBlock
        {
            Text = "Esta herramienta del trabajo suelto se conserva y vive aquí, bajo «Herramientas». " +
                   "Su porte desde la versión anterior es el siguiente tramo de trabajo; el flujo " +
                   "«Preparar Evento» es la prioridad del rediseño.",
            Foreground = (Brush)Application.Current.Resources["FmcText2"],
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
        });

        Content = panel;
    }
}
