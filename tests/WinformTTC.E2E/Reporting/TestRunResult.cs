namespace WinformTTC.E2E.Reporting;

public sealed class TestRunResult
{
    public string TestName { get; }
    public DateTime StartTime { get; }
    public DateTime EndTime { get; private set; }
    public IReadOnlyList<TestStepResult> Steps { get; private set; } = [];
    public bool AllPassed => Steps.Count > 0 && Steps.All(s => s.Passed);
    public TimeSpan Duration => EndTime - StartTime;

    public TestRunResult(string testName)
    {
        TestName = testName;
        StartTime = DateTime.Now;
    }

    public void Complete(IReadOnlyList<TestStepResult> steps)
    {
        Steps = steps;
        EndTime = DateTime.Now;
    }
}
