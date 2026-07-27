using System.Windows.Controls;
using FormatMediaCoder.App.ViewModels;

namespace FormatMediaCoder.App.Views;

public partial class ConvertView : UserControl
{
    public ConvertView()
    {
        InitializeComponent();
        DataContext = new ConvertViewModel();
    }
}
