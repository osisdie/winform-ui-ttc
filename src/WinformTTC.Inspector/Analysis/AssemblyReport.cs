namespace WinformTTC.Inspector.Analysis;

/// <summary>
/// Top-level report for an inspected assembly.
/// </summary>
public sealed record AssemblyReport(
    string AssemblyName,
    string AssemblyPath,
    IReadOnlyList<FormReport> Forms,
    IReadOnlyList<string> Warnings);
