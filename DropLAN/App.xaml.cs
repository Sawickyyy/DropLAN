using Velopack;

namespace DropLAN;

public partial class App : System.Windows.Application
{
    [STAThread]
    private static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}