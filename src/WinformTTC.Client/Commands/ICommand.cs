using WinformTTC.Client.Automation;

namespace WinformTTC.Client.Commands;

public sealed record CommandResult(bool Success, string Message);

public interface ICommand
{
    string Description { get; }
    Task<CommandResult> ExecuteAsync(AppController controller, IProgress<string> progress);
}
