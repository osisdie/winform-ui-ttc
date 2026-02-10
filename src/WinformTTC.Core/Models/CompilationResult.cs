namespace WinformTTC.Core.Models;

public sealed record CompilationResult(
    bool Success,
    byte[]? AssemblyBytes,
    IReadOnlyList<string> Diagnostics);
