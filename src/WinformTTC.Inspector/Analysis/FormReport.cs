namespace WinformTTC.Inspector.Analysis;

/// <summary>
/// Report for a single Form type discovered in the assembly.
/// </summary>
public sealed record FormReport(
    string TypeName,
    string? Title,
    IReadOnlyList<ControlInfo> Controls);
