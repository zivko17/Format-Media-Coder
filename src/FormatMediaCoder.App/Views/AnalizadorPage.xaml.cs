using Microsoft.UI.Xaml.Controls;
using FormatMediaCoder.App.ViewModels;
using Windows.Storage.Pickers;

namespace FormatMediaCoder.App.Views;

public sealed partial class AnalizadorPage : Page
{
    private readonly AnalizadorViewModel _vm = new();

    public AnalizadorPage()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private async void PickFile_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add("*");
        WinRT.Interop.InitializeWithWindow.Initialize(picker, MainWindow.Hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is not null) _vm.InputFile = file.Path;
    }
}
