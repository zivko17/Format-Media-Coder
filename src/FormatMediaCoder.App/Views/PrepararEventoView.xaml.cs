using System.Windows.Controls;
using FormatMediaCoder.App.ViewModels;
using Microsoft.Win32;

namespace FormatMediaCoder.App.Views;

public partial class PrepararEventoView : UserControl
{
    private readonly PrepararEventoViewModel _vm = new();

    public PrepararEventoView()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private void PickFolder_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        // .NET 8 trae OpenFolderDialog nativo en Microsoft.Win32.
        var dialog = new OpenFolderDialog { Title = "Elige la carpeta con el material del cliente" };
        if (dialog.ShowDialog() == true)
            _vm.InputFolder = dialog.FolderName;
    }
}
