using System.Windows.Controls;
using FormatMediaCoder.App.ViewModels;

namespace FormatMediaCoder.App.Views;

public partial class AnalizadorView : UserControl
{
    public AnalizadorView()
    {
        InitializeComponent();
        DataContext = new AnalizadorViewModel();
    }
}
