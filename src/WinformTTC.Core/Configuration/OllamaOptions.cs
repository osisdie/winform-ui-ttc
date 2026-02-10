namespace WinformTTC.Core.Configuration;

public sealed class OllamaOptions
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string ModelId { get; set; } = "qwen2.5-coder:7b-instruct-q5_K_M";
    public int TimeoutSeconds { get; set; } = 120;
}
