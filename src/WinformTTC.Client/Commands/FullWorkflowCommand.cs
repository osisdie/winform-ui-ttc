using WinformTTC.Client.Automation;

namespace WinformTTC.Client.Commands;

public sealed class FullWorkflowCommand : ICommand
{
    private readonly string _prompt;

    public FullWorkflowCommand(string prompt)
    {
        _prompt = prompt;
    }

    public string Description => $"Full workflow: Generate + Compile & Run for \"{_prompt}\"";

    public async Task<CommandResult> ExecuteAsync(AppController controller, IProgress<string> progress)
    {
        try
        {
            var (code, output) = await controller.FullWorkflowAsync(_prompt, progress);

            if (string.IsNullOrEmpty(code))
                return new CommandResult(false, "Generation produced no code.");

            if (string.IsNullOrEmpty(output))
                return new CommandResult(false, $"Generated {code.Length} chars but execution produced no output.");

            return new CommandResult(true, $"Generated {code.Length} chars. Output: {output}");
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"Workflow failed: {ex.Message}");
        }
    }
}
