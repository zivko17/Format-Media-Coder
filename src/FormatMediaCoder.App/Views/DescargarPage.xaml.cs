using Microsoft.UI.Xaml.Controls;
using FormatMediaCoder.App.ViewModels;

namespace FormatMediaCoder.App.Views;

public sealed partial class DescargarPage : Page
{
    private readonly DescargarViewModel _vm = new();

    public DescargarPage()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private async void PickFolder_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var path = await Pickers.PickFolderAsync();
        if (path is not null) _vm.OutputDir = path;
    }
}
