using WinformTTC.Core.Models;

namespace WinformTTC.Core.Services;

public interface ICompilationService
{
    CompilationResult Compile(string sourceCode);
    Task<ExecutionResult> ExecuteAsync(byte[] assemblyBytes, CancellationToken cancellationToken);
}
