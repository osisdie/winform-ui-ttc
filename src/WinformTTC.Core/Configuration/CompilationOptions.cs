namespace WinformTTC.Core.Configuration;

public sealed class CompilationOptions
{
    public int ExecutionTimeoutSeconds { get; set; } = 30;
    public bool AllowUnsafeCode { get; set; }
}
