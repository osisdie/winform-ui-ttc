using System.Reflection;
using System.Runtime.Loader;

namespace WinformTTC.Inspector.Loading;

/// <summary>
/// Custom AssemblyLoadContext that probes the target assembly's directory
/// for satellite DLLs (e.g. Scintilla.NET). Falls back to default context
/// for BCL/WinForms assemblies.
/// </summary>
internal sealed class InspectionLoadContext : AssemblyLoadContext
{
    private readonly string _probingDirectory;

    public InspectionLoadContext(string targetAssemblyPath)
        : base(isCollectible: false)
    {
        _probingDirectory = Path.GetDirectoryName(Path.GetFullPath(targetAssemblyPath))
            ?? throw new ArgumentException("Cannot determine directory for target assembly.", nameof(targetAssemblyPath));
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Try to find the assembly in the target's directory
        var candidatePath = Path.Combine(_probingDirectory, $"{assemblyName.Name}.dll");
        if (File.Exists(candidatePath))
        {
            return LoadFromAssemblyPath(candidatePath);
        }

        // Fall back to default context (BCL, WinForms, etc.)
        return null;
    }
}
