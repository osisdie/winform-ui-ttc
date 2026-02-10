using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Microsoft.Extensions.Configuration;
using WinformTTC.E2E.Logging;
using Xunit;

namespace WinformTTC.E2E.Infrastructure;

public class AppFixture : IAsyncLifetime
{
    private FlaUI.Core.Application? _application;
    private UIA3Automation? _automation;

    public FlaUI.Core.Application Application => _application ?? throw new InvalidOperationException("Application not started");
    public UIA3Automation Automation => _automation ?? throw new InvalidOperationException("Automation not initialized");
    public Window MainWindow { get; private set; } = null!;
    public IConfiguration Configuration { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.e2e.json", optional: true)
            .Build();

        var appPath = ResolveAppPath();
        TestLogger.Log.Information("Launching application: {Path}", appPath);

        _automation = new UIA3Automation();
        _application = FlaUI.Core.Application.Launch(appPath);

        var windowTimeout = TimeSpan.FromSeconds(
            Configuration.GetValue("Timeouts:WindowLoadSeconds", 30));

        var found = await WaitHelpers.WaitUntilAsync(() =>
        {
            try
            {
                var window = _application.GetMainWindow(_automation);
                return window != null && !string.IsNullOrEmpty(window.Title);
            }
            catch
            {
                return false;
            }
        }, windowTimeout);

        if (!found)
            throw new TimeoutException($"Main window did not appear within {windowTimeout.TotalSeconds}s");

        MainWindow = _application.GetMainWindow(_automation)
            ?? throw new InvalidOperationException("Main window was null after successful wait");
        TestLogger.Log.Information("Main window found: {Title}", MainWindow.Title);
    }

    public Task DisposeAsync()
    {
        TestLogger.Log.Information("Shutting down application");

        try
        {
            _application?.Close();

            if (_application != null && !_application.HasExited)
            {
                _application.Kill();
            }
        }
        catch (Exception ex)
        {
            TestLogger.Log.Warning(ex, "Error during application shutdown");
        }

        _automation?.Dispose();
        return Task.CompletedTask;
    }

    private string ResolveAppPath()
    {
        var configuredPath = Configuration["AppPath"];
        if (!string.IsNullOrEmpty(configuredPath) && File.Exists(configuredPath))
            return configuredPath;

        // Convention: look relative to test output directory
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..",
                "src", "WinformTTC.App", "bin", "Debug", "net10.0-windows", "WinformTTC.App.exe")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..",
                "src", "WinformTTC.App", "bin", "Debug", "net10.0", "WinformTTC.App.exe")),
        };

        foreach (var candidate in candidates)
        {
            TestLogger.Log.Debug("Checking app path: {Path}", candidate);
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException(
            $"WinformTTC.App.exe not found. Build the app first or set AppPath in appsettings.e2e.json. " +
            $"Searched: {string.Join(", ", candidates)}");
    }
}
