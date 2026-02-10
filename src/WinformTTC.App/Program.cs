using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WinformTTC.App.Configuration;
using WinformTTC.App.Forms;
using WinformTTC.App.ViewModels;
using WinformTTC.Core;

namespace WinformTTC.App;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<EditorOptions>(context.Configuration.GetSection("Editor"));
                services.AddWinformTtcCore(context.Configuration);
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainForm>();
            })
            .Build();

        var form = host.Services.GetRequiredService<MainForm>();
        Application.Run(form);
    }
}
