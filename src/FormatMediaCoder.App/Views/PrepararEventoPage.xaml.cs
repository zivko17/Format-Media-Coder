using Microsoft.UI.Xaml.Controls;
using FormatMediaCoder.App.ViewModels;
using Windows.Storage.Pickers;

namespace FormatMediaCoder.App.Views;

public sealed partial class PrepararEventoPage : Page
{
    private readonly PrepararEventoViewModel _vm = new();

    public PrepararEventoPage()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private async void PickFolder_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        // WinUI: hay que asociar el picker con la ventana.
        WinRT.Interop.InitializeWithWindow.Initialize(picker, MainWindow.Hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null) _vm.InputFolder = folder.Path;
    }
}
