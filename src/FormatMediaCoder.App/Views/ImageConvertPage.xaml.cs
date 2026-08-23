using Microsoft.UI.Xaml.Controls;
using FormatMediaCoder.App.ViewModels;

namespace FormatMediaCoder.App.Views;

public sealed partial class ImageConvertPage : Page
{
    private readonly ImageConvertViewModel _vm = new();

    public ImageConvertPage()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private async void PickFile_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var path = await Pickers.PickFileAsync(".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tif", ".tiff", ".gif");
        if (path is not null) _vm.InputFile = path;
    }
}
