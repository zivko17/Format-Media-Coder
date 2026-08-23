using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using FormatMediaCoder.App.Views;

namespace FormatMediaCoder.App;

public sealed partial class MainWindow : Window
{
    /// <summary>Handle de la ventana, que necesitan los file pickers de WinUI.</summary>
    public static nint Hwnd { get; private set; }

    public MainWindow()
    {
        InitializeComponent();

        Hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        Title = "Format Media Coder";

        // Fondo Mica y barra de título extendida (look Fluent nativo).
        SystemBackdrop = new MicaBackdrop();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Abrir en Preparar Evento.
        Nav.SelectedItem = Nav.MenuItems.OfType<NavigationViewItem>().FirstOrDefault();
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item) return;
        var tag = item.Tag as string ?? "";

        ContentHost.Content = tag switch
        {
            "prep" => new PrepararEventoPage(),
            "convert" => new ConvertPage(),
            "analizador" => new AnalizadorPage(),
            _ when tag.StartsWith("tool:") => new ToolPlaceholderPage(tag[5..]),
            _ => ContentHost.Content,
        };
    }
}
