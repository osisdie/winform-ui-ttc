using WinformTTC.Client.Automation;

namespace WinformTTC.Client.Commands;

public sealed class CompileRunCommand : ICommand
{
    public string Description => "Compile & Run the current code";

    public async Task<CommandResult> ExecuteAsync(AppController controller, IProgress<string> progress)
    {
        try
        {
            var output = await controller.CompileRunAndWaitAsync(progress);
            return string.IsNullOrEmpty(output)
                ? new CommandResult(false, "Compile/Run produced no output.")
                : new CommandResult(true, $"Output: {output}");
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"Compile/Run failed: {ex.Message}");
        }
    }
}
