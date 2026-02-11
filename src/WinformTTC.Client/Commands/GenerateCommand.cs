using WinformTTC.Client.Automation;

namespace WinformTTC.Client.Commands;

public sealed class GenerateCommand : ICommand
{
    private readonly string _prompt;

    public GenerateCommand(string prompt)
    {
        _prompt = prompt;
    }

    public string Description => $"Generate code for \"{_prompt}\"";

    public async Task<CommandResult> ExecuteAsync(AppController controller, IProgress<string> progress)
    {
        try
        {
            var code = await controller.GenerateAndWaitAsync(_prompt, progress);
            return string.IsNullOrEmpty(code)
                ? new CommandResult(false, "Generation produced no code.")
                : new CommandResult(true, $"Generated {code.Length} characters of code.");
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"Generate failed: {ex.Message}");
        }
    }
}
