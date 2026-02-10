using FlaUI.Core.AutomationElements;
using Microsoft.Extensions.Configuration;
using WinformTTC.E2E.Logging;
using WinformTTC.E2E.Reporting;

namespace WinformTTC.E2E.Infrastructure;

public abstract class FlaUITestBase : IDisposable
{
    protected AppFixture Fixture { get; }
    protected Window MainWindow => Fixture.MainWindow;
    protected IConfiguration Config => Fixture.Configuration;

    private readonly TestRunResult _runResult;
    private readonly List<TestStepResult> _steps = [];

    protected FlaUITestBase(AppFixture fixture, string testName)
    {
        Fixture = fixture;
        _runResult = new TestRunResult(testName);
        TestLogger.Log.Information("Starting test: {TestName}", testName);
    }

    protected void RecordStep(string stepName, bool passed, string detail = "")
    {
        var screenshotPath = ScreenshotHelper.CaptureWindow(MainWindow, stepName);
        var step = new TestStepResult(stepName, passed, detail, screenshotPath);
        _steps.Add(step);

        var level = passed ? Serilog.Events.LogEventLevel.Information : Serilog.Events.LogEventLevel.Error;
        TestLogger.Log.Write(level, "Step [{Result}]: {Step} - {Detail}",
            passed ? "PASS" : "FAIL", stepName, detail);
    }

    protected string GetStatusText() => MainWindow.GetStatusText();

    protected async Task WaitForStatus(string expectedStatus, TimeSpan? timeout = null)
    {
        var waitTimeout = timeout ?? TimeSpan.FromSeconds(
            Config.GetValue("Timeouts:DefaultWaitSeconds", 10));

        var found = await WaitHelpers.WaitForStatusAsync(
            () => GetStatusText(),
            expectedStatus,
            waitTimeout);

        if (!found)
        {
            var actual = GetStatusText();
            TestLogger.Log.Warning("Status wait timeout. Expected: {Expected}, Actual: {Actual}",
                expectedStatus, actual);
        }
    }

    public void Dispose()
    {
        _runResult.Complete(_steps);
        TestLogger.Log.Information("Test {TestName} completed: {Result}",
            _runResult.TestName,
            _runResult.AllPassed ? "PASSED" : "FAILED");

        try
        {
            HtmlReportGenerator.Generate(_runResult);
        }
        catch (Exception ex)
        {
            TestLogger.Log.Error(ex, "Failed to generate HTML report");
        }

        GC.SuppressFinalize(this);
    }
}
