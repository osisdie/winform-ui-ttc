using FlaUI.Core.AutomationElements;
using WinformTTC.Client.Configuration;

namespace WinformTTC.Client.Automation;

/// <summary>
/// High-level workflow methods that operate on the WinformTTC.App via FlaUI.
/// </summary>
public sealed class AppController
{
    private readonly AppConnector _connector;
    private readonly ClientOptions _options;

    public AppController(AppConnector connector, ClientOptions options)
    {
        _connector = connector;
        _options = options;
    }

    private Window Window => _connector.MainWindow
        ?? throw new InvalidOperationException("Not connected to WinformTTC.App");

    public async Task<string> GenerateAndWaitAsync(string prompt, IProgress<string>? progress = null)
    {
        progress?.Report("Setting prompt text...");
        Window.SetPromptText(prompt);

        progress?.Report("Clicking Generate...");
        var generateBtn = Window.FindToolStripButton("Generate")
            ?? throw new InvalidOperationException("Generate button not found");
        generateBtn.ClickToolStripButton();

        // Run all UIA polling on a background thread so the Client UI
        // stays responsive while the target App is busy generating.
        progress?.Report("Waiting for generation to start...");
        var timeout = TimeSpan.FromSeconds(_options.GenerationTimeoutSeconds);
        var window = Window;

        await Task.Run(async () =>
        {
            await WaitHelpers.WaitForStatusContainsAsync(
                () => window.GetStatusText(), "Generating", TimeSpan.FromSeconds(10));
        });

        progress?.Report("Waiting for generation to complete...");
        var completed = await Task.Run(async () =>
        {
            return await WaitHelpers.WaitForStatusAsync(
                () => window.GetStatusText(), "Code generation completed.", timeout);
        });

        if (!completed)
        {
            progress?.Report("Generation timed out.");
            return string.Empty;
        }

        var codeEditor = Window.FindByAccessibleName("Code Editor");
        var code = codeEditor != null ? codeEditor.GetScintillaText(Window) : string.Empty;
        progress?.Report($"Generation completed. Code: {code.Length} chars");
        return code;
    }

    public async Task<string> CompileRunAndWaitAsync(IProgress<string>? progress = null)
    {
        progress?.Report("Clicking Compile & Run...");
        var compileBtn = Window.FindToolStripButton("Compile & Run")
            ?? throw new InvalidOperationException("Compile & Run button not found");
        compileBtn.ClickToolStripButton();

        var window = Window;
        var timeout = TimeSpan.FromSeconds(_options.CompilationTimeoutSeconds);

        progress?.Report("Waiting for compilation...");
        await Task.Run(async () =>
        {
            await WaitHelpers.WaitForStatusContainsAsync(
                () => window.GetStatusText(), "Compil", TimeSpan.FromSeconds(10));
        });

        progress?.Report("Waiting for execution to complete...");
        var completed = await Task.Run(async () =>
        {
            return await WaitHelpers.WaitForStatusAsync(
                () => window.GetStatusText(), "Execution completed.", timeout);
        });

        if (!completed)
        {
            progress?.Report("Compile/Run timed out.");
            return string.Empty;
        }

        var outputDisplay = Window.FindByAccessibleName("Output Display");
        var output = outputDisplay != null ? outputDisplay.GetTextBoxText() : string.Empty;
        progress?.Report($"Execution completed. Output: {output.Length} chars");
        return output;
    }

    public async Task<(string Code, string Output)> FullWorkflowAsync(string prompt, IProgress<string>? progress = null)
    {
        var code = await GenerateAndWaitAsync(prompt, progress);
        if (string.IsNullOrEmpty(code))
            return (code, string.Empty);

        var output = await CompileRunAndWaitAsync(progress);
        return (code, output);
    }

    public void Stop(IProgress<string>? progress = null)
    {
        progress?.Report("Clicking Stop...");
        var stopBtn = Window.FindToolStripButton("Stop");
        if (stopBtn != null)
        {
            stopBtn.ClickToolStripButton();
            progress?.Report("Stop requested.");
        }
        else
        {
            progress?.Report("Stop button not found.");
        }
    }
}
