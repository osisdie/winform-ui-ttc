namespace WinformTTC.Inspector.Analysis;

/// <summary>
/// Information about a single UI control discovered via reflection.
/// </summary>
public sealed record ControlInfo(
    string FieldName,
    string TypeName,
    string? AccessibleName,
    IReadOnlyDictionary<string, string?> Properties,
    IReadOnlyList<ControlInfo> Children);
