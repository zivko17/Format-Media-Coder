using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FormatMediaCoder.App.Views;

/// <summary>
/// Sitio de cada una de las nueve herramientas del original dentro del nuevo
/// menú (brief §6.2: no se tiran, se reagrupan bajo "Herramientas"). El porte de
/// cada pantalla desde la versión anterior es trabajo aparte; esta vista deja
/// claro dónde va y que sigue viva.
/// </summary>
public sealed class ToolPlaceholderView : UserControl
{
    public ToolPlaceholderView(string toolName)
    {
        Background = (Brush)Application.Current.Resources["BgApp"];

        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = 460,
        };

        panel.Children.Add(new TextBlock
        {
            Text = toolName,
            Foreground = (Brush)Application.Current.Resources["Text1"],
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10),
        });

        panel.Children.Add(new TextBlock
        {
            Text = "Esta herramienta del trabajo suelto se conserva y vive aquí, bajo «Herramientas». " +
                   "Su porte desde la versión anterior es el siguiente tramo de trabajo; el flujo «Preparar Evento» es la prioridad del rediseño.",
            Foreground = (Brush)Application.Current.Resources["Text2"],
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
        });

        Content = panel;
    }
}
