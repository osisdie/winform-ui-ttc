using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;
using WinformTTC.Core.Configuration;
using WinformTTC.Core.Services;

namespace WinformTTC.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWinformTtcCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OllamaOptions>()
            .Bind(configuration.GetSection("Ollama"));

        services.AddOptions<CompilationOptions>()
            .Bind(configuration.GetSection("Compilation"));

        services.AddSingleton<Kernel>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OllamaOptions>>().Value;
            var builder = Kernel.CreateBuilder();
            builder.AddOllamaChatCompletion(options.ModelId, new Uri(options.Endpoint));
            return builder.Build();
        });

        services.AddSingleton<ICodeGenerationService, CodeGenerationService>();
        services.AddSingleton<ICompilationService, RoslynCompilationService>();

        return services;
    }
}
