using Microsoft.UI.Xaml.Controls;
using FormatMediaCoder.App.ViewModels;

namespace FormatMediaCoder.App.Views;

public sealed partial class EdicionPage : Page
{
    private readonly EdicionViewModel _vm = new();

    public EdicionPage()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private async void PickFile_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var path = await Pickers.PickFileAsync(".mp4", ".mov", ".mkv", ".avi", ".webm", ".wmv", ".m4v");
        if (path is not null) _vm.InputFile = path;
    }

    private async void AddMerge_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var paths = await Pickers.PickFilesAsync(".mp4", ".mov", ".mkv", ".avi", ".webm", ".wmv", ".m4v");
        foreach (var p in paths) _vm.MergeFiles.Add(p);
        _vm.RaiseMergeCanExecute();
    }
}
