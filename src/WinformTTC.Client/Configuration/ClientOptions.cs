namespace WinformTTC.Client.Configuration;

public sealed class ClientOptions
{
    public string AppPath { get; set; } = string.Empty;
    public int WindowLoadTimeoutSeconds { get; set; } = 30;
    public int GenerationTimeoutSeconds { get; set; } = 120;
    public int CompilationTimeoutSeconds { get; set; } = 30;
    public int DefaultPollIntervalMs { get; set; } = 250;
}
