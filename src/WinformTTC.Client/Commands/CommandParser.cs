using System.Text.RegularExpressions;

namespace WinformTTC.Client.Commands;

/// <summary>
/// Parses natural-language input into ICommand instances using regex patterns.
/// </summary>
public static partial class CommandParser
{
    public static ICommand Parse(string input)
    {
        input = input.Trim();

        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Empty command.");

        // "stop" or "cancel"
        if (StopPattern().IsMatch(input))
            return new StopCommand();

        // "compile and run" or "compile & run"
        if (CompileRunPattern().IsMatch(input))
            return new CompileRunCommand();

        // "generate only <prompt>" → generate without compile & run
        var genOnlyMatch = GenerateOnlyPattern().Match(input);
        if (genOnlyMatch.Success)
        {
            var prompt = genOnlyMatch.Groups[1].Value.Trim();
            return new GenerateCommand(prompt);
        }

        // "generate the code `1+1=?`" → full workflow (generate + compile + run)
        var fullMatch = FullWorkflowPattern().Match(input);
        if (fullMatch.Success)
        {
            var prompt = fullMatch.Groups[1].Value;
            return new FullWorkflowCommand(prompt);
        }

        // "generate <prompt>" → full workflow (most useful default)
        var genMatch = GeneratePattern().Match(input);
        if (genMatch.Success)
        {
            var prompt = genMatch.Groups[1].Value.Trim();
            return new FullWorkflowCommand(prompt);
        }

        // Fallback: treat entire input as a prompt for full workflow
        return new FullWorkflowCommand(input);
    }

    [GeneratedRegex(@"^(?:stop|cancel)$", RegexOptions.IgnoreCase)]
    private static partial Regex StopPattern();

    [GeneratedRegex(@"^compile\s*(?:&|and)\s*run$", RegexOptions.IgnoreCase)]
    private static partial Regex CompileRunPattern();

    [GeneratedRegex(@"^generate\s+only\s+(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex GenerateOnlyPattern();

    [GeneratedRegex(@"generate\s+(?:the\s+)?code\s*[`""'](.+?)[`""']", RegexOptions.IgnoreCase)]
    private static partial Regex FullWorkflowPattern();

    [GeneratedRegex(@"^generate\s+(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex GeneratePattern();
}
