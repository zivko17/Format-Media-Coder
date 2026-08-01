using System.Windows;
using System.Windows.Controls;
using FormatMediaCoder.App.ViewModels;

namespace FormatMediaCoder.App.Views;

public partial class PdfToPptxView : UserControl
{
    private readonly PdfToPptxViewModel _vm = new();

    public PdfToPptxView()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    // Arrastrar y soltar los PDFs directamente sobre la ventana.
    private void Root_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Root_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
            _vm.AddFiles(paths);
        e.Handled = true;
    }
}
