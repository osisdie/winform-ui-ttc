namespace WinformTTC.E2E.Reporting;

public sealed class TestStepResult(
    string stepName,
    bool passed,
    string detail,
    string screenshotPath)
{
    public string StepName { get; } = stepName;
    public bool Passed { get; } = passed;
    public string Detail { get; } = detail;
    public string ScreenshotPath { get; } = screenshotPath;
    public DateTime Timestamp { get; } = DateTime.Now;
}
