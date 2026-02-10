using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Options;
using WinformTTC.Core.Configuration;
using WinformTTC.Core.Models;

namespace WinformTTC.Core.Services;

public sealed class RoslynCompilationService : ICompilationService
{
    private readonly WinformTTC.Core.Configuration.CompilationOptions _options;
    private readonly IReadOnlyList<MetadataReference> _references;

    public RoslynCompilationService(IOptions<WinformTTC.Core.Configuration.CompilationOptions> options)
    {
        _options = options.Value;
        _references = ResolveReferences();
    }

    public CompilationResult Compile(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            sourceCode,
            new CSharpParseOptions(LanguageVersion.Preview));

        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.ConsoleApplication,
            optimizationLevel: OptimizationLevel.Release,
            allowUnsafe: _options.AllowUnsafeCode);

        var compilation = CSharpCompilation.Create(
            $"UserProgram_{Guid.NewGuid():N}",
            new[] { syntaxTree },
            _references,
            compilationOptions);

        using var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);

        if (!emitResult.Success)
        {
            var diagnostics = emitResult.Diagnostics
                .Where(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
                .Select(d => d.ToString())
                .ToArray();

            return new CompilationResult(false, null, diagnostics);
        }

        return new CompilationResult(true, stream.ToArray(), Array.Empty<string>());
    }

    public async Task<ExecutionResult> ExecuteAsync(byte[] assemblyBytes, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(_options.ExecutionTimeoutSeconds);
        var outputWriter = new StringWriter();
        var originalOut = Console.Out;
        var originalErr = Console.Error;

        var context = new SandboxAssemblyLoadContext();
        try
        {
            using var assemblyStream = new MemoryStream(assemblyBytes);
            var assembly = context.LoadFromStream(assemblyStream);
            var entryPoint = assembly.EntryPoint;
            if (entryPoint is null)
            {
                return new ExecutionResult(false, string.Empty, "No entry point found.");
            }

            Console.SetOut(outputWriter);
            Console.SetError(outputWriter);

            var parameters = entryPoint.GetParameters().Length == 0
                ? null
                : new object?[] { Array.Empty<string>() };

            var invocationTask = Task.Run(async () =>
            {
                var result = entryPoint.Invoke(null, parameters);
                if (result is Task task)
                {
                    await task.ConfigureAwait(false);
                }
            }, cancellationToken);

            var completed = await Task.WhenAny(invocationTask, Task.Delay(timeout, cancellationToken));
            if (completed != invocationTask)
            {
                return new ExecutionResult(false, outputWriter.ToString(), "Execution timed out.");
            }

            await invocationTask;
            return new ExecutionResult(true, outputWriter.ToString(), null);
        }
        catch (OperationCanceledException)
        {
            return new ExecutionResult(false, outputWriter.ToString(), "Cancelled");
        }
        catch (TargetInvocationException ex)
        {
            var message = ex.InnerException?.ToString() ?? ex.ToString();
            return new ExecutionResult(false, outputWriter.ToString(), message);
        }
        catch (Exception ex)
        {
            return new ExecutionResult(false, outputWriter.ToString(), ex.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
            context.Unload();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    private static IReadOnlyList<MetadataReference> ResolveReferences()
    {
        var tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (string.IsNullOrWhiteSpace(tpa))
        {
            return Array.Empty<MetadataReference>();
        }

        var references = tpa.Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();

        return references;
    }

    private sealed class SandboxAssemblyLoadContext : AssemblyLoadContext
    {
        public SandboxAssemblyLoadContext()
            : base(isCollectible: true)
        {
        }

        protected override Assembly? Load(AssemblyName assemblyName) => null;
    }
}
