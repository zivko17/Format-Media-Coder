using Microsoft.UI.Xaml.Controls;
using FormatMediaCoder.App.ViewModels;

namespace FormatMediaCoder.App.Views;

public sealed partial class AudioConvertPage : Page
{
    private readonly AudioConvertViewModel _vm = new();

    public AudioConvertPage()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private async void PickFile_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var path = await Pickers.PickFileAsync(".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a", ".mp4", ".mov", ".mkv");
        if (path is not null) _vm.InputFile = path;
    }
}
