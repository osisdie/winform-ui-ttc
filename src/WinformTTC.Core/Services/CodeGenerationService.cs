using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using WinformTTC.Core.Configuration;
using WinformTTC.Core.Models;

namespace WinformTTC.Core.Services;

public sealed class CodeGenerationService : ICodeGenerationService
{
    private const string SystemPrompt = """
You are a C# code generator. Return only compilable C# code.
- Include a static Main entry point.
- Do not include markdown fences or explanations.
- Target .NET 10.
""";

    private readonly IChatCompletionService _chatCompletionService;
    public CodeGenerationService(Kernel kernel, IOptions<OllamaOptions> options)
    {
        _chatCompletionService = kernel.Services.GetRequiredService<IChatCompletionService>();
        _ = options.Value;
    }

    public async IAsyncEnumerable<string> GenerateCodeStreamingAsync(
        string prompt,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var chat = new ChatHistory(SystemPrompt);
        chat.AddUserMessage(prompt);

        await foreach (var message in _chatCompletionService.GetStreamingChatMessageContentsAsync(
                           chat,
                           cancellationToken: cancellationToken))
        {
            if (!string.IsNullOrEmpty(message.Content))
            {
                yield return message.Content;
            }
        }
    }

    public async Task<CodeGenerationResult> GenerateCodeAsync(string prompt, CancellationToken cancellationToken)
    {
        try
        {
            var chat = new ChatHistory(SystemPrompt);
            chat.AddUserMessage(prompt);

            var response = await _chatCompletionService.GetChatMessageContentAsync(chat, cancellationToken: cancellationToken);
            var code = CodeExtractor.ExtractCSharpCode(response.Content ?? string.Empty);

            return new CodeGenerationResult(true, code, null);
        }
        catch (OperationCanceledException)
        {
            return new CodeGenerationResult(false, string.Empty, "Cancelled");
        }
        catch (Exception ex)
        {
            return new CodeGenerationResult(false, string.Empty, ex.Message);
        }
    }
}
