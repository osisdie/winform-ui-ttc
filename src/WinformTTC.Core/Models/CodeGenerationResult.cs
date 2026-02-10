namespace WinformTTC.Core.Models;

public sealed record CodeGenerationResult(
    bool Success,
    string Code,
    string? ErrorMessage);
