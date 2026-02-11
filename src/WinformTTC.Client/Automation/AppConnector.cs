using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using WinformTTC.Client.Configuration;

namespace WinformTTC.Client.Automation;

/// <summary>
/// Connects to a running WinformTTC.App process or launches a new one.
/// Modeled after the E2E AppFixture but adapted for attach/detach semantics.
/// </summary>
public sealed class AppConnector : IDisposable
{
    private FlaUI.Core.Application? _application;
    private UIA3Automation? _automation;
    private bool _launchedByUs;

    public Window? MainWindow { get; private set; }
    public bool IsConnected => MainWindow != null && _application != null;
    public int? ProcessId => _application?.ProcessId;

    public async Task AttachAsync(ClientOptions options, IProgress<string>? progress = null)
    {
        if (IsConnected)
            throw new InvalidOperationException("Already connected. Detach first.");

        _automation = new UIA3Automation();

        // Try to find a running instance first
        var processes = Process.GetProcessesByName("WinformTTC.App");
        if (processes.Length > 0)
        {
            var process = processes[0];
            _application = FlaUI.Core.Application.Attach(process);
            _launchedByUs = false;
            progress?.Report($"Attached to existing WinformTTC.App (PID {process.Id})");
        }
        else
        {
            var appPath = ResolveAppPath(options.AppPath);
            progress?.Report($"Launching WinformTTC.App from {appPath}...");
            _application = FlaUI.Core.Application.Launch(appPath);
            _launchedByUs = true;
            progress?.Report($"Launched WinformTTC.App (PID {_application.ProcessId})");
        }

        var timeout = TimeSpan.FromSeconds(options.WindowLoadTimeoutSeconds);
        progress?.Report("Waiting for main window...");

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
        }, timeout);

        if (!found)
        {
            Dispose();
            throw new TimeoutException($"Main window did not appear within {timeout.TotalSeconds}s");
        }

        MainWindow = _application.GetMainWindow(_automation)
            ?? throw new InvalidOperationException("Main window was null after successful wait");

        progress?.Report($"Connected to {MainWindow.Title} (PID {_application.ProcessId})");
    }

    public void Detach(IProgress<string>? progress = null)
    {
        if (!IsConnected)
            return;

        progress?.Report("Detaching...");
        MainWindow = null;
        _application = null;
        _automation?.Dispose();
        _automation = null;
        _launchedByUs = false;
        progress?.Report("Detached.");
    }

    public void Dispose()
    {
        if (_launchedByUs && _application != null)
        {
            try
            {
                _application.Close();
                if (!_application.HasExited)
                    _application.Kill();
            }
            catch
            {
                // Best effort cleanup
            }
        }

        _application = null;
        MainWindow = null;
        _automation?.Dispose();
        _automation = null;
    }

    private static string ResolveAppPath(string configuredPath)
    {
        if (!string.IsNullOrEmpty(configuredPath) && File.Exists(configuredPath))
            return configuredPath;

        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..",
                "src", "WinformTTC.App", "bin", "Debug", "net10.0-windows", "WinformTTC.App.exe")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..",
                "WinformTTC.App", "bin", "Debug", "net10.0-windows", "WinformTTC.App.exe")),
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException(
            $"WinformTTC.App.exe not found. Build the app first or set AppPath in appsettings.client.json. " +
            $"Searched: {string.Join(", ", candidates)}");
    }
}
