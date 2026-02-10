using WinformTTC.Core.Models;

namespace WinformTTC.Core.Services;

public interface ICodeGenerationService
{
    IAsyncEnumerable<string> GenerateCodeStreamingAsync(string prompt, CancellationToken cancellationToken);
    Task<CodeGenerationResult> GenerateCodeAsync(string prompt, CancellationToken cancellationToken);
}
