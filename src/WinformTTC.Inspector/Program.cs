using WinformTTC.Inspector.Analysis;
using WinformTTC.Inspector.Output;

namespace WinformTTC.Inspector;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: WinformTTC.Inspector <path-to-dll-or-exe> [--output <report.md>]");
            return 1;
        }

        var assemblyPath = args[0];
        string? outputPath = null;

        for (var i = 1; i < args.Length - 1; i++)
        {
            if (args[i] is "--output" or "-o")
            {
                outputPath = args[i + 1];
                break;
            }
        }

        try
        {
            var report = AssemblyAnalyzer.Analyze(assemblyPath);
            IReportWriter writer = new MarkdownReportWriter();
            var markdown = writer.Write(report);

            if (outputPath != null)
            {
                File.WriteAllText(outputPath, markdown);
                Console.WriteLine($"Report written to {outputPath}");
            }
            else
            {
                Console.Write(markdown);
            }

            return 0;
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 2;
        }
    }
}
