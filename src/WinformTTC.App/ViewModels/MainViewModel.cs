using System.Net;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using WinformTTC.Core.Configuration;
using WinformTTC.Core.Services;

namespace WinformTTC.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly ICodeGenerationService _codeGenerationService;
    private readonly ICompilationService _compilationService;
    private readonly OllamaOptions _ollamaOptions;
    private CancellationTokenSource? _cancellation;

    [ObservableProperty]
    private string promptText = string.Empty;

    [ObservableProperty]
    private string generatedCode = string.Empty;

    [ObservableProperty]
    private string outputText = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private string diagnostics = string.Empty;

    [ObservableProperty]
    private bool isProcessing;

    [ObservableProperty]
    private string modelId;

    public MainViewModel(
        ICodeGenerationService codeGenerationService,
        ICompilationService compilationService,
        IOptions<OllamaOptions> ollamaOptions)
    {
        _codeGenerationService = codeGenerationService;
        _compilationService = compilationService;
        _ollamaOptions = ollamaOptions.Value;
        modelId = _ollamaOptions.ModelId;

        GenerateCodeCommand = new AsyncRelayCommand(GenerateCodeAsync, CanGenerate);
        CompileAndRunCommand = new AsyncRelayCommand(CompileAndRunAsync, CanCompileAndRun);
        StopCommand = new RelayCommand(Stop, CanStop);
    }

    public IAsyncRelayCommand GenerateCodeCommand { get; }
    public IAsyncRelayCommand CompileAndRunCommand { get; }
    public IRelayCommand StopCommand { get; }

    private bool CanGenerate() => !IsProcessing;
    private bool CanCompileAndRun() => !IsProcessing && !string.IsNullOrWhiteSpace(GeneratedCode);
    private bool CanStop() => IsProcessing;

    private async Task GenerateCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(PromptText))
        {
            StatusMessage = "Enter a prompt to generate code.";
            return;
        }

        SetProcessing("Generating code...");
        OutputText = string.Empty;
        Diagnostics = string.Empty;
        GeneratedCode = string.Empty;

        _cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(_ollamaOptions.TimeoutSeconds));
        var builder = new StringBuilder();

        try
        {
            await foreach (var chunk in _codeGenerationService.GenerateCodeStreamingAsync(
                               PromptText,
                               _cancellation.Token))
            {
                builder.Append(chunk);
                GeneratedCode = builder.ToString();
            }

            StatusMessage = "Code generation completed.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = "Code generation failed.";
            OutputText = GetGenerationErrorMessage(ex);
        }
        finally
        {
            ClearProcessing();
        }
    }

    private async Task CompileAndRunAsync()
    {
        if (string.IsNullOrWhiteSpace(GeneratedCode))
        {
            StatusMessage = "Generate or paste code first.";
            return;
        }

        SetProcessing("Compiling...");
        OutputText = string.Empty;
        Diagnostics = string.Empty;
        _cancellation = new CancellationTokenSource();

        try
        {
            var sourceCode = CodeExtractor.ExtractCSharpCode(GeneratedCode);
            if (!string.Equals(sourceCode, GeneratedCode, StringComparison.Ordinal))
            {
                GeneratedCode = sourceCode;
            }

            var compilation = _compilationService.Compile(sourceCode);
            if (!compilation.Success || compilation.AssemblyBytes is null)
            {
                Diagnostics = string.Join(Environment.NewLine, compilation.Diagnostics);
                OutputText = Diagnostics;
                StatusMessage = "Compilation failed.";
                return;
            }

            StatusMessage = "Running...";
            var execution = await _compilationService.ExecuteAsync(compilation.AssemblyBytes, _cancellation.Token);

            if (!execution.Success)
            {
                OutputText = string.Join(Environment.NewLine, new[]
                {
                    execution.Output,
                    execution.ErrorMessage ?? string.Empty
                }.Where(text => !string.IsNullOrWhiteSpace(text)));

                StatusMessage = execution.ErrorMessage is null ? "Execution failed." : execution.ErrorMessage;
                return;
            }

            OutputText = execution.Output;
            StatusMessage = "Execution completed.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Cancelled";
        }
        catch (Exception ex)
        {
            OutputText = ex.ToString();
            StatusMessage = "Execution failed.";
        }
        finally
        {
            ClearProcessing();
        }
    }

    private void Stop()
    {
        _cancellation?.Cancel();
    }

    private void SetProcessing(string status)
    {
        IsProcessing = true;
        StatusMessage = status;
        UpdateCommandState();
    }

    private void ClearProcessing()
    {
        IsProcessing = false;
        UpdateCommandState();
    }

    partial void OnGeneratedCodeChanged(string value)
    {
        UpdateCommandState();
    }

    partial void OnIsProcessingChanged(bool value)
    {
        UpdateCommandState();
    }

    private void UpdateCommandState()
    {
        GenerateCodeCommand.NotifyCanExecuteChanged();
        CompileAndRunCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
    }

    private string GetGenerationErrorMessage(Exception ex)
    {
        if (ex is HttpRequestException httpEx)
        {
            if (httpEx.StatusCode == HttpStatusCode.NotFound)
            {
                return $"Model not found: {_ollamaOptions.ModelId}. Run: ollama pull {_ollamaOptions.ModelId}";
            }

            return $"Cannot connect to Ollama at {_ollamaOptions.Endpoint}.";
        }

        if (ex.Message.Contains("404", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("Not Found", StringComparison.OrdinalIgnoreCase))
        {
            return $"Model not found: {_ollamaOptions.ModelId}. Run: ollama pull {_ollamaOptions.ModelId}";
        }

        return ex.Message;
    }
}
