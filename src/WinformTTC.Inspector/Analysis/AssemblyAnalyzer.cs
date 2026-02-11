using System.Reflection;
using System.Windows.Forms;
using WinformTTC.Inspector.Loading;

namespace WinformTTC.Inspector.Analysis;

/// <summary>
/// Orchestrator: loads an assembly, finds all Form types, and produces an AssemblyReport.
/// </summary>
internal static class AssemblyAnalyzer
{
    public static AssemblyReport Analyze(string assemblyPath)
    {
        var fullPath = Path.GetFullPath(assemblyPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Assembly not found: {fullPath}");

        var warnings = new List<string>();
        var context = new InspectionLoadContext(fullPath);

        Assembly assembly;
        try
        {
            assembly = context.LoadFromAssemblyPath(fullPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load assembly: {fullPath}", ex);
        }

        var formTypes = GetFormTypes(assembly, warnings);
        var reports = new List<FormReport>();

        foreach (var formType in formTypes)
        {
            try
            {
                var report = FormInspector.Inspect(formType);
                reports.Add(report);
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to inspect form '{formType.FullName}': {ex.Message}");
            }
        }

        var assemblyName = assembly.GetName().Name ?? Path.GetFileNameWithoutExtension(fullPath);
        return new AssemblyReport(assemblyName, fullPath, reports, warnings);
    }

    private static IReadOnlyList<Type> GetFormTypes(Assembly assembly, List<string> warnings)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null).ToArray()!;
            foreach (var loaderEx in ex.LoaderExceptions.Where(e => e != null))
            {
                warnings.Add($"Type load warning: {loaderEx!.Message}");
            }
        }

        return types
            .Where(t => t.IsClass && !t.IsAbstract && typeof(Form).IsAssignableFrom(t))
            .ToArray();
    }
}
