using System.Reflection;
using System.Windows.Forms;

namespace WinformTTC.Inspector.Analysis;

/// <summary>
/// Inspects a single Form type via reflection to extract its control hierarchy.
/// No instantiation is attempted — only field and type metadata is analyzed.
/// </summary>
internal static class FormInspector
{
    private static readonly Type ControlType = typeof(Control);
    private static readonly Type ToolStripItemType = typeof(ToolStripItem);

    public static FormReport Inspect(Type formType)
    {
        var title = GetFormTitle(formType);
        var controls = ScanFields(formType);
        return new FormReport(formType.FullName ?? formType.Name, title, controls);
    }

    private static string? GetFormTitle(Type formType)
    {
        // Try to read the default Text property value from InitializeComponent via field defaults
        // Since we can't instantiate, look for a [DesignerSerializationVisibility] or similar hint
        // Best-effort: check if there's a static/const Title field, otherwise return null
        var textProp = formType.GetProperty("Text", BindingFlags.Public | BindingFlags.Instance);
        if (textProp != null)
        {
            // We can't call the getter without an instance, so return the type name as a hint
            return null;
        }
        return null;
    }

    private static IReadOnlyList<ControlInfo> ScanFields(Type type)
    {
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var controls = new List<ControlInfo>();

        foreach (var field in fields)
        {
            var fieldType = field.FieldType;

            if (IsControlType(fieldType) || IsToolStripItemType(fieldType))
            {
                var info = BuildControlInfo(field);
                controls.Add(info);
            }
        }

        return controls;
    }

    private static ControlInfo BuildControlInfo(FieldInfo field)
    {
        var fieldType = field.FieldType;
        var properties = ExtractTypeProperties(fieldType);
        var accessibleName = GetAccessibleNameDefault(fieldType);
        var children = GetChildFieldTypes(fieldType);

        return new ControlInfo(
            FieldName: field.Name,
            TypeName: fieldType.FullName ?? fieldType.Name,
            AccessibleName: accessibleName,
            Properties: properties,
            Children: children);
    }

    private static IReadOnlyDictionary<string, string?> ExtractTypeProperties(Type fieldType)
    {
        var props = new Dictionary<string, string?>();

        // Extract interesting property types/defaults from the type metadata
        var relevantProps = new[] { "Text", "Name", "AccessibleName", "Dock", "Multiline", "ReadOnly", "Enabled", "Visible" };

        foreach (var propName in relevantProps)
        {
            var prop = fieldType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
            {
                props[propName] = prop.PropertyType.Name;
            }
        }

        return props;
    }

    private static string? GetAccessibleNameDefault(Type fieldType)
    {
        // Without instantiation, we can only note that the property exists
        var prop = fieldType.GetProperty("AccessibleName", BindingFlags.Public | BindingFlags.Instance);
        return prop != null ? "(set at runtime)" : null;
    }

    private static IReadOnlyList<ControlInfo> GetChildFieldTypes(Type containerType)
    {
        // For container types like ToolStrip, SplitContainer, etc., check if they
        // have a known Items/Controls collection property — but we can't enumerate
        // actual children without instantiation. Return empty.
        return [];
    }

    private static bool IsControlType(Type type)
    {
        return type != null && ControlType.IsAssignableFrom(type);
    }

    private static bool IsToolStripItemType(Type type)
    {
        return type != null && ToolStripItemType.IsAssignableFrom(type);
    }
}
