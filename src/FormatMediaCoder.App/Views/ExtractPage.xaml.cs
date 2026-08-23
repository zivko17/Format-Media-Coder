using Microsoft.UI.Xaml.Controls;
using FormatMediaCoder.App.ViewModels;

namespace FormatMediaCoder.App.Views;

public sealed partial class ExtractPage : Page
{
    private readonly ExtractViewModel _vm = new();

    public ExtractPage()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private async void PickFile_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var path = await Pickers.PickFileAsync(".mp4", ".mov", ".mkv", ".avi", ".webm", ".wmv", ".m4v");
        if (path is not null) _vm.InputFile = path;
    }
}
