using System.Windows;
using Velopack;

namespace DropLAN;

public partial class App : System.Windows.Application
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Velopack musi wystartować możliwie najwcześniej.
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();

        var settings = AppSettings.Load();

        if (string.IsNullOrWhiteSpace(settings.Language))
        {
            var languageWindow = new LanguageWindow();

            if (languageWindow.ShowDialog() != true ||
                string.IsNullOrWhiteSpace(languageWindow.SelectedLanguage))
            {
                app.Shutdown();
                return;
            }

            settings.Language = languageWindow.SelectedLanguage;
            settings.Save();
        }

        AppSettings.ApplyLanguage(settings.Language!);

        var mainWindow = new MainWindow();
        app.MainWindow = mainWindow;
        mainWindow.Show();

        app.Run();
    }
}
