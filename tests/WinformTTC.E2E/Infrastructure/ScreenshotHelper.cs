using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using WinformTTC.E2E.Logging;

namespace WinformTTC.E2E.Infrastructure;

public static class ScreenshotHelper
{
    private static readonly string ScreenshotDir = Path.Combine(AppContext.BaseDirectory, "screenshots");

    static ScreenshotHelper()
    {
        Directory.CreateDirectory(ScreenshotDir);
    }

    public static string CaptureWindow(Window window, string stepName)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        var safeName = string.Join("_", stepName.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"{timestamp}_{safeName}.png";
        var filePath = Path.Combine(ScreenshotDir, fileName);

        try
        {
            var image = Capture.Element(window);
            image.ToFile(filePath);
            TestLogger.Log.Information("Screenshot saved: {Path}", filePath);
        }
        catch (Exception ex)
        {
            TestLogger.Log.Warning(ex, "Failed to capture screenshot for step {Step}", stepName);
            // Fallback to full screen capture
            try
            {
                var screen = Capture.Screen();
                screen.ToFile(filePath);
                TestLogger.Log.Information("Fallback screen capture saved: {Path}", filePath);
            }
            catch (Exception ex2)
            {
                TestLogger.Log.Error(ex2, "Full screen capture also failed for step {Step}", stepName);
                return string.Empty;
            }
        }

        return filePath;
    }
}
