using Microsoft.UI.Xaml.Controls;
using FormatMediaCoder.App.ViewModels;
using Windows.Storage.Pickers;

namespace FormatMediaCoder.App.Views;

public sealed partial class ConvertPage : Page
{
    private readonly ConvertViewModel _vm = new();

    public ConvertPage()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private async void PickFile_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        foreach (var ext in new[] { ".mp4", ".mov", ".mkv", ".avi", ".webm", ".wmv", ".m4v", ".mpg", ".mpeg", ".flv" })
            picker.FileTypeFilter.Add(ext);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, MainWindow.Hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is not null) _vm.InputFile = file.Path;
    }
}
