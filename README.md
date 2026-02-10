# Text-to-Code Engine — WinForms (.NET 10) + Roslyn + Local LLM

## Context

Build a WinForms desktop application that accepts natural language input, generates C# code via a local LLM (Ollama), compiles and executes the code in-memory using Roslyn, and displays results. The application prioritizes **data privacy** (all local), **fast iteration** (WinForms for engineers), and **integration with existing .NET systems**.

The repo is greenfield — only infrastructure files exist (`Directory.Build.props`, `global.json`, `.editorconfig`). Target: .NET 10.0 LTS (SDK 10.0.102).

---

## Solution Structure

```
ai-winform-ui/
├── Directory.Build.props          (MODIFY — add NoWarn for SK experimental APIs)
├── global.json                    (existing)
├── .editorconfig                  (existing)
├── WinformTTC.sln                 (NEW)
├── src/
│   ├── WinformTTC.Core/           (class library — net10.0)
│   │   ├── WinformTTC.Core.csproj
│   │   ├── Configuration/
│   │   │   ├── OllamaOptions.cs
│   │   │   └── CompilationOptions.cs
│   │   ├── Models/
│   │   │   ├── CodeGenerationResult.cs
│   │   │   ├── CompilationResult.cs
│   │   │   └── ExecutionResult.cs
│   │   ├── Services/
│   │   │   ├── ICodeGenerationService.cs
│   │   │   ├── ICompilationService.cs
│   │   │   ├── CodeGenerationService.cs
│   │   │   ├── CodeExtractor.cs
│   │   │   └── RoslynCompilationService.cs
│   │   └── ServiceCollectionExtensions.cs
│   └── WinformTTC.App/            (WinForms — net10.0-windows)
│       ├── WinformTTC.App.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── Configuration/
│       │   └── EditorOptions.cs
│       ├── ViewModels/
│       │   └── MainViewModel.cs
│       ├── Forms/
│       │   ├── MainForm.cs
│       │   └── MainForm.Designer.cs
│       └── Controls/
│           └── ScintillaConfigurator.cs
└── tests/                         (future)
```

**Why two projects?** Core has zero Windows dependencies — reusable in CLI/API/tests. App is a thin UI shell. This is the minimum useful separation without over-engineering.

---

## NuGet Packages

### WinformTTC.Core (`net10.0`)
| Package | Purpose |
|---------|---------|
| `Microsoft.SemanticKernel` | LLM orchestration (Kernel, plugins, chat) |
| `Microsoft.SemanticKernel.Connectors.Ollama` | Local model connector (prerelease) |
| `Microsoft.CodeAnalysis.CSharp` | Roslyn C# compilation |
| `Microsoft.Extensions.Options` | Options pattern for configuration |
| `Microsoft.Extensions.Logging.Abstractions` | Logging |

### WinformTTC.App (`net10.0-windows`)
| Package | Purpose |
|---------|---------|
| `CommunityToolkit.Mvvm` | MVVM source generators (ObservableProperty, RelayCommand) |
| `Scintilla.NET.WinForms` | Code editor with C# syntax highlighting |
| `Microsoft.Extensions.Hosting` | Generic host (DI + config + logging) |

---

## Key Components

### 1. RoslynCompilationService — `src/WinformTTC.Core/Services/RoslynCompilationService.cs`

The safety-critical component. Implements `ICompilationService`.

- **Compile**: `CSharpSyntaxTree.ParseText` → `CSharpCompilation.Create` → `Emit` to `MemoryStream` → return `byte[]` or diagnostics
- **Execute**: Load `byte[]` into collectible `AssemblyLoadContext` → find `Main` entry point → redirect `Console.Out` to `StringWriter` → `Task.Run` with timeout → capture output → unload context
- **References**: Resolved from `TRUSTED_PLATFORM_ASSEMBLIES` (System.Runtime, System.Console, System.Linq, System.Collections, etc.)
- **Sandbox**: `file sealed class SandboxAssemblyLoadContext : AssemblyLoadContext` with `isCollectible: true`

### 2. CodeGenerationService — `src/WinformTTC.Core/Services/CodeGenerationService.cs`

Uses Semantic Kernel's `IChatCompletionService` via the Ollama connector.

- **System prompt**: Instructs LLM to return only compilable C# with `Main` entry point
- **Streaming**: `IAsyncEnumerable<string>` via `GetStreamingChatMessageContentsAsync` for progressive UI display
- **Code extraction**: `CodeExtractor.ExtractCSharpCode()` strips markdown fences from LLM output

### 3. MainViewModel — `src/WinformTTC.App/ViewModels/MainViewModel.cs`

Orchestrates UI state via CommunityToolkit.Mvvm source generators.

- **Properties**: `PromptText`, `GeneratedCode`, `OutputText`, `StatusMessage`, `IsProcessing`, `Diagnostics`
- **Commands**: `GenerateCodeCommand`, `CompileAndRunCommand`, `StopCommand` (with CanExecute guards)
- **Cancellation**: Single `CancellationTokenSource` managed per operation

### 4. MainForm — `src/WinformTTC.App/Forms/MainForm.cs`

WinForms UI with no business logic. Binds to ViewModel via `PropertyChanged` + `InvokeAsync`.

---

## UI Layout

```
+------------------------------------------------------------------+
| [ToolStrip]  [Generate] [Compile & Run] [Stop]   [Model: ____v]  |
+------------------------------------------------------------------+
| Natural Language Input (TextBox, Multiline, ~3 lines)             |
+==================================================================+
|                                                                   |
|  Generated Code (ScintillaNET)                                    |
|  - C# syntax highlighting, line numbers, editable                 |
|                                                                   |
+==================================================================+
| Output / Results (RichTextBox)                                    |
| - stdout, compilation errors (red), status messages               |
+------------------------------------------------------------------+
| [StatusStrip]  Ready  |  ████░░  |  Model: qwen2.5-coder:7b     |
+------------------------------------------------------------------+
```

Layout: `SplitContainer` (horizontal) nested — outer splits input vs rest, inner splits editor vs output. All controls use `Dock: Fill` for resizability.

---

## Data Flow

```
User types prompt → [Generate] →
  ViewModel.GenerateCodeAsync():
    1. IsProcessing = true, create CancellationTokenSource
    2. CodeGenerationService.GenerateCodeStreamingAsync(prompt, ct)
    3. Stream chunks → update GeneratedCode → ScintillaNET refreshes
    4. IsProcessing = false

User reviews/edits code → [Compile & Run] →
  ViewModel.CompileAndRunAsync():
    1. IsProcessing = true
    2. CompilationService.Compile(code)
    3. If errors → display diagnostics + highlight error lines
    4. CompilationService.ExecuteAsync(result, ct)
    5. OutputText = stdout or error message
    6. IsProcessing = false

[Stop] → CancellationTokenSource.Cancel()
```

**Threading**: All Semantic Kernel / Roslyn work runs on background threads. UI updates via `PropertyChanged` → `Control.InvokeAsync` marshalling. `IProgress<string>` for streaming updates.

---

## Error Handling

| Scenario | Handling |
|----------|----------|
| Ollama not running | `HttpRequestException` → friendly message: "Cannot connect to Ollama" |
| Model not found | Surface Ollama error → suggest `ollama list` |
| Generation timeout | `CancellationTokenSource.CreateLinkedTokenSource` + `CancelAfter(120s)` |
| Compilation errors | `EmitResult.Diagnostics` → red indicators in ScintillaNET + error list |
| Runtime exception | `TargetInvocationException` → display `InnerException.Message` + stack trace |
| Execution timeout | `Task.WaitAsync(30s)` → "Execution timed out" |
| User cancellation | `OperationCanceledException` → StatusMessage = "Cancelled" |

---

## Configuration — `appsettings.json`

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "ModelId": "qwen2.5-coder:7b-instruct-q5_K_M",
    "TimeoutSeconds": 120
  },
  "Compilation": {
    "ExecutionTimeoutSeconds": 30,
    "AllowUnsafeCode": false
  },
  "Editor": {
    "FontFamily": "Consolas",
    "FontSize": 11
  }
}
```

Options pattern: `OllamaOptions`, `CompilationOptions`, `EditorOptions` POCOs bound via `IOptions<T>`.

---

## Files to Modify

| File | Change |
|------|--------|
| `Directory.Build.props` | Add `<NoWarn>$(NoWarn);SKEXP0001;SKEXP0010;SKEXP0070</NoWarn>` for SK experimental APIs |

---

## Implementation Phases

### Phase 1: Scaffolding
- Modify `Directory.Build.props` (add NoWarn)
- Create `WinformTTC.sln`, both `.csproj` files, `Program.cs`
- Verify `dotnet build` succeeds

### Phase 2: Models & Interfaces
- All records in `Models/` (CodeGenerationResult, CompilationResult, ExecutionResult)
- Service interfaces (`ICodeGenerationService`, `ICompilationService`)
- Options classes (`OllamaOptions`, `CompilationOptions`)

### Phase 3: Roslyn Compilation Service
- `RoslynCompilationService.Compile()` with reference resolution
- `SandboxAssemblyLoadContext` (collectible)
- `ExecuteAsync()` with Console.Out redirect + timeout
- Test with hardcoded C# string

### Phase 4: Code Generation Service
- `CodeExtractor` (strip markdown fences)
- `CodeGenerationService` (Semantic Kernel + Ollama)
- `ServiceCollectionExtensions.AddWinformTtcCore()`

### Phase 5: WinForms UI
- `MainViewModel` (properties, commands, async flow)
- `ScintillaConfigurator` (C# highlighting, line numbers, error indicators)
- `MainForm` layout (SplitContainers, toolbar, status bar)
- ViewModel ↔ Form binding via PropertyChanged + InvokeAsync
- `appsettings.json` + DI wiring in `Program.cs`

### Phase 6: Integration & Polish
- End-to-end test: prompt → generate → compile → run → output
- Error line highlighting in editor
- Streaming code display
- Progress bar in status strip

---

## Verification

1. **Build**: `dotnet build WinformTTC.sln` — zero errors, zero warnings
2. **Roslyn standalone**: Hardcode a C# string, verify compile + execute returns expected output
3. **Ollama connectivity**: Run `ollama serve` + `ollama pull qwen2.5-coder:7b-instruct-q5_K_M`, verify CodeGenerationService returns valid C#
4. **End-to-end**: Type "write a program that prints fibonacci numbers up to 100" → Generate → code appears in editor → Compile & Run → fibonacci output appears
5. **Error paths**: Submit invalid prompt, verify graceful error display; test Stop button mid-generation; test without Ollama running
6. **Memory**: Run multiple generate/compile/run cycles, verify no assembly leak (collectible ALC unloads)
