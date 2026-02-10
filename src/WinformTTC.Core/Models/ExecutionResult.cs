namespace WinformTTC.Core.Models;

public sealed record ExecutionResult(
    bool Success,
    string Output,
    string? ErrorMessage);
