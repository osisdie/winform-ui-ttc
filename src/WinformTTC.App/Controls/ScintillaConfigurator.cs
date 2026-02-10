using ScintillaNet.Abstractions.Classes.Lexers;
using ScintillaNet.Abstractions.Enumerations;
using ScintillaNet.WinForms;
using WinformTTC.App.Configuration;

namespace WinformTTC.App.Controls;

public static class ScintillaConfigurator
{
    private const int ErrorMarker = 1;

    public static void ApplyCSharpConfiguration(Scintilla editor, EditorOptions options)
    {
        editor.StyleResetDefault();
        editor.Styles[Cpp.Default].Font = options.FontFamily;
        editor.Styles[Cpp.Default].Size = options.FontSize;
        editor.StyleClearAll();

        editor.LexerName = "cpp";
        editor.SetKeywords(0, "abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null object operator out override params private protected public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void while");

        editor.Styles[Cpp.Comment].ForeColor = System.Drawing.Color.Green;
        editor.Styles[Cpp.CommentLine].ForeColor = System.Drawing.Color.Green;
        editor.Styles[Cpp.CommentDoc].ForeColor = System.Drawing.Color.DarkGreen;
        editor.Styles[Cpp.Number].ForeColor = System.Drawing.Color.Olive;
        editor.Styles[Cpp.String].ForeColor = System.Drawing.Color.Brown;
        editor.Styles[Cpp.Character].ForeColor = System.Drawing.Color.Brown;
        editor.Styles[Cpp.Word].ForeColor = System.Drawing.Color.Blue;
        editor.Styles[Cpp.Word2].ForeColor = System.Drawing.Color.Blue;
        editor.Styles[Cpp.Operator].ForeColor = System.Drawing.Color.Purple;

        editor.Margins[0].Width = 40;
        editor.Margins[0].Type = MarginType.Number;

        editor.Markers[ErrorMarker].Symbol = MarkerSymbol.Background;
        editor.Markers[ErrorMarker].SetBackColor(System.Drawing.Color.LightPink);
    }

    public static void SetErrorMarkers(Scintilla editor, IEnumerable<int> lineNumbers)
    {
        ClearErrorMarkers(editor);
        foreach (var line in lineNumbers)
        {
            if (line >= 0 && line < editor.Lines.Count)
            {
                editor.Lines[line].MarkerAdd(ErrorMarker);
            }
        }
    }

    public static void ClearErrorMarkers(Scintilla editor)
    {
        editor.MarkerDeleteAll(ErrorMarker);
    }
}
