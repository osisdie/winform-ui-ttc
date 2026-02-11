using WinformTTC.Client.Automation;

namespace WinformTTC.Client.Commands;

public sealed class StopCommand : ICommand
{
    public string Description => "Stop the current operation";

    public Task<CommandResult> ExecuteAsync(AppController controller, IProgress<string> progress)
    {
        try
        {
            controller.Stop(progress);
            return Task.FromResult(new CommandResult(true, "Stop requested."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new CommandResult(false, $"Stop failed: {ex.Message}"));
        }
    }
}
