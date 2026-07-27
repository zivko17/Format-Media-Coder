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
        // Las herramientas del original se reagrupan aquí (brief §6.2). Se van
        // portando desde la versión anterior; las ya portadas abren su vista real,
        // el resto muestran de momento su sitio en el nuevo menú.
        var name = (sender as Button)?.Content?.ToString() ?? "Herramienta";
        MainContent.Content = name switch
        {
            _ when name.Contains("Convertir Vídeo") => new ConvertView(),
            _ when name.Contains("Analizador") => new AnalizadorView(),
            _ => new ToolPlaceholderView(name),
        };
    }
}
