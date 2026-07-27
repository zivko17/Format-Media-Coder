using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace FormatMediaCoder.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Si algo revienta al arrancar, que se vea y quede registrado, en vez de
        // cerrarse en silencio sin ventana.
        DispatcherUnhandledException += (_, ev) => { Report(ev.Exception, "UI"); ev.Handled = true; };
        AppDomain.CurrentDomain.UnhandledException += (_, ev) =>
        {
            if (ev.ExceptionObject is Exception ex) Report(ex, "dominio");
        };

        base.OnStartup(e);

        try
        {
            new MainWindow().Show();
        }
        catch (Exception ex)
        {
            Report(ex, "arranque");
            Shutdown(1);
        }
    }

    private static void Report(Exception ex, string origen)
    {
        var msg = $"[{origen}] {ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}";
        try
        {
            var log = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "fmc-error.log");
            File.AppendAllText(log, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n{msg}\n\n");
        }
        catch { /* si ni siquiera se puede loguear, al menos el MessageBox */ }

        MessageBox.Show(msg, "Format Media Coder — error de arranque",
            MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
