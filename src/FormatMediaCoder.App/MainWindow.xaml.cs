using System.Windows;
using System.Windows.Controls;
using FormatMediaCoder.App.Views;

namespace FormatMediaCoder.App;

public partial class MainWindow : Window
{
    private readonly PrepararEventoView _prepararView = new();

    public MainWindow()
    {
        InitializeComponent();
        // Preparar Evento es la pantalla que se abre al arrancar (brief §6.1).
        MainContent.Content = _prepararView;
    }

    private void NavPreparar_Click(object sender, RoutedEventArgs e)
    {
        MainContent.Content = _prepararView;
    }

    private void NavTool_Click(object sender, RoutedEventArgs e)
    {
        // Las nueve herramientas del original no se tiran: se reagrupan aquí
        // (brief §6.2). El porte de cada una desde la versión anterior es trabajo
        // aparte; de momento se muestra su sitio en el nuevo menú.
        var name = (sender as Button)?.Content?.ToString() ?? "Herramienta";
        MainContent.Content = new ToolPlaceholderView(name);
    }
}
