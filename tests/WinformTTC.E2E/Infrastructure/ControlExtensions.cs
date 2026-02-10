using System.Windows.Forms;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using WinformTTC.E2E.Logging;

namespace WinformTTC.E2E.Infrastructure;

public static class ControlExtensions
{
    /// <summary>
    /// Finds a ToolStripButton by its Name property in the UIA tree.
    /// ToolStripItems expose their Text as Name in automation.
    /// </summary>
    public static AutomationElement? FindToolStripButton(this Window window, string name)
    {
        return window.FindFirstDescendant(cf =>
            cf.ByName(name).And(cf.ByControlType(ControlType.Button)));
    }

    /// <summary>
    /// Clicks a ToolStripButton reliably using mouse input at its clickable point.
    /// FlaUI's default Click/Invoke can be unreliable for ToolStripButton controls.
    /// </summary>
    public static void ClickToolStripButton(this AutomationElement button)
    {
        var point = button.GetClickablePoint();
        Mouse.Click(point);
        Wait.UntilInputIsProcessed();
    }

    /// <summary>
    /// Finds a control by its AccessibleName.
    /// Falls back to searching by ControlType for known control types.
    /// </summary>
    public static AutomationElement? FindByAccessibleName(this Window window, string accessibleName)
    {
        // Direct search by Name (maps to AccessibleName in WinForms)
        var element = window.FindFirstDescendant(cf => cf.ByName(accessibleName));
        if (element != null)
            return element;

        // Fallback for RichTextBox: search by ControlType.Document
        // RichTextBox inside nested SplitContainers may not expose AccessibleName in UIA
        if (accessibleName == "Output Display")
        {
            element = window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Document));
            if (element != null)
            {
                TestLogger.Log.Debug("Found Output Display via ControlType.Document fallback");
                return element;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets text from a standard TextBox or RichTextBox via UIA ValuePattern.
    /// </summary>
    public static string GetTextBoxText(this AutomationElement element)
    {
        if (element.Patterns.Value.TryGetPattern(out var valuePattern))
            return valuePattern.Value.Value ?? string.Empty;

        if (element.Patterns.Text.TryGetPattern(out var textPattern))
            return textPattern.DocumentRange.GetText(-1) ?? string.Empty;

        return element.Name ?? string.Empty;
    }

    /// <summary>
    /// Gets text from the Scintilla editor via clipboard.
    /// Scintilla is a native Win32 control that doesn't expose text through UIA patterns.
    /// </summary>
    public static string GetScintillaText(this AutomationElement scintilla)
    {
        try
        {
            scintilla.Focus();
            Wait.UntilInputIsProcessed();

            // Store current clipboard
            string? previousClipboard = null;
            RunOnStaThread(() => previousClipboard = Clipboard.GetText());

            // Select all and copy
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            Wait.UntilInputIsProcessed();
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_C);
            Wait.UntilInputIsProcessed();
            Thread.Sleep(100); // Small delay for clipboard sync

            string text = string.Empty;
            RunOnStaThread(() => text = Clipboard.GetText());

            // Restore clipboard
            if (previousClipboard != null)
            {
                RunOnStaThread(() => Clipboard.SetText(previousClipboard));
            }

            // Deselect
            Keyboard.Press(VirtualKeyShort.RIGHT);
            Wait.UntilInputIsProcessed();

            return text;
        }
        catch (Exception ex)
        {
            TestLogger.Log.Warning(ex, "Failed to get Scintilla text via clipboard");
            return string.Empty;
        }
    }

    /// <summary>
    /// Gets the status bar text from the StatusStrip's first label.
    /// Without AccessibleName set, UIA Name reflects the actual Text property.
    /// </summary>
    public static string GetStatusText(this Window window)
    {
        var statusBar = window.FindFirstDescendant(cf => cf.ByControlType(ControlType.StatusBar));
        if (statusBar != null)
        {
            var firstChild = statusBar.FindFirstChild();
            return firstChild?.Name ?? string.Empty;
        }

        return string.Empty;
    }

    /// <summary>
    /// Types text into the prompt TextBox.
    /// Uses ValuePattern for speed, then appends/removes a character to ensure
    /// the WinForms TextChanged event fires (ValuePattern.SetValue bypasses it).
    /// </summary>
    public static void SetPromptText(this Window window, string text)
    {
        var promptBox = window.FindByAccessibleName("Prompt Input");
        if (promptBox == null)
            throw new InvalidOperationException("Could not find Prompt Input control");

        promptBox.Focus();
        Wait.UntilInputIsProcessed();

        if (promptBox.Patterns.Value.TryGetPattern(out var valuePattern))
        {
            valuePattern.SetValue(text);
            Wait.UntilInputIsProcessed();

            // ValuePattern.SetValue may not fire TextChanged in WinForms.
            // Append a space then delete it to force the event with the correct final text.
            Keyboard.Press(VirtualKeyShort.END);
            Wait.UntilInputIsProcessed();
            Keyboard.Type(" ");
            Wait.UntilInputIsProcessed();
            Keyboard.Press(VirtualKeyShort.BACK);
            Wait.UntilInputIsProcessed();
        }
        else
        {
            // Fallback: type via keyboard
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            Wait.UntilInputIsProcessed();
            Keyboard.Type(text);
        }

        Wait.UntilInputIsProcessed();
    }

    private static void RunOnStaThread(Action action)
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            action();
            return;
        }

        Exception? threadException = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { threadException = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null)
            throw threadException;
    }
}
