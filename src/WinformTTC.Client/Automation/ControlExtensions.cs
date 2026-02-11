using System.Windows.Forms;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace WinformTTC.Client.Automation;

public static class ControlExtensions
{
    public static AutomationElement? FindToolStripButton(this Window window, string name)
    {
        return window.FindFirstDescendant(cf =>
            cf.ByName(name).And(cf.ByControlType(ControlType.Button)));
    }

    public static void ClickToolStripButton(this AutomationElement button)
    {
        // Bring the parent window to the foreground so the physical mouse
        // click lands on the correct window (not the Client overlapping it).
        var parent = button.Parent;
        while (parent != null && parent.ControlType != FlaUI.Core.Definitions.ControlType.Window)
            parent = parent.Parent;
        (parent as Window)?.Focus();
        Thread.Sleep(150);

        // Use physical mouse click at the button's clickable point.
        // InvokePattern is intentionally NOT used here because it blocks
        // synchronously until the button's async handler completes — for
        // long-running operations like code generation this causes a UIA
        // COM timeout (~20 s) before the caller can proceed.
        var point = button.GetClickablePoint();
        Mouse.Click(point);
        Wait.UntilInputIsProcessed();
    }

    public static AutomationElement? FindByAccessibleName(this Window window, string accessibleName)
    {
        var element = window.FindFirstDescendant(cf => cf.ByName(accessibleName));
        if (element != null)
            return element;

        if (accessibleName == "Output Display")
        {
            element = window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Document));
            if (element != null)
                return element;
        }

        return null;
    }

    public static string GetTextBoxText(this AutomationElement element)
    {
        if (element.Patterns.Value.TryGetPattern(out var valuePattern))
            return valuePattern.Value.Value ?? string.Empty;

        if (element.Patterns.Text.TryGetPattern(out var textPattern))
            return textPattern.DocumentRange.GetText(-1) ?? string.Empty;

        return element.Name ?? string.Empty;
    }

    public static string GetScintillaText(this AutomationElement scintilla, Window? parentWindow = null)
    {
        try
        {
            // Ensure the target window is in front for keyboard input.
            parentWindow?.Focus();
            Thread.Sleep(100);

            scintilla.Focus();
            Wait.UntilInputIsProcessed();

            string? previousClipboard = null;
            RunOnStaThread(() => previousClipboard = Clipboard.GetText());

            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            Wait.UntilInputIsProcessed();
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_C);
            Wait.UntilInputIsProcessed();
            Thread.Sleep(100);

            string text = string.Empty;
            RunOnStaThread(() => text = Clipboard.GetText());

            if (previousClipboard != null)
            {
                RunOnStaThread(() => Clipboard.SetText(previousClipboard));
            }

            Keyboard.Press(VirtualKeyShort.RIGHT);
            Wait.UntilInputIsProcessed();

            return text;
        }
        catch
        {
            return string.Empty;
        }
    }

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

    public static void SetPromptText(this Window window, string text)
    {
        // Ensure the target App window is in the foreground before
        // sending keyboard input — otherwise keystrokes go to Client.
        window.Focus();
        Thread.Sleep(100);

        var promptBox = window.FindByAccessibleName("Prompt Input")
            ?? throw new InvalidOperationException("Could not find Prompt Input control");

        promptBox.Focus();
        Wait.UntilInputIsProcessed();

        if (promptBox.Patterns.Value.TryGetPattern(out var valuePattern))
        {
            valuePattern.SetValue(text);
            Wait.UntilInputIsProcessed();

            Keyboard.Press(VirtualKeyShort.END);
            Wait.UntilInputIsProcessed();
            Keyboard.Type(" ");
            Wait.UntilInputIsProcessed();
            Keyboard.Press(VirtualKeyShort.BACK);
            Wait.UntilInputIsProcessed();
        }
        else
        {
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
