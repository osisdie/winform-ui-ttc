# Text-to-Code Engine — WinForms (.NET 10) + Roslyn + Local LLM

A WinForms desktop application that accepts natural language input, generates C# code via a local LLM (Ollama), compiles and executes the code in-memory using Roslyn, and displays results. The solution also includes an assembly inspector and a FlaUI-based remote-control client. All local, zero cloud dependencies.

**Target:** .NET 10.0 LTS (SDK 10.0.102)

---

## Solution Structure

```
ai-winform-ui/
├── Directory.Build.props              # Shared: net10.0, TreatWarningsAsErrors, EnableWindowsTargeting
├── global.json                        # SDK 10.0.102
├── WinformTTC.sln                     # Classic solution format
├── WinformTTC.slnx                    # Modern XML solution format
├── src/
│   ├── WinformTTC.Core/               # Class library (net10.0) — code gen + compilation
│   │   ├── Configuration/             # OllamaOptions, CompilationOptions
│   │   ├── Models/                    # CodeGenerationResult, CompilationResult, ExecutionResult
│   │   ├── Services/                  # Roslyn compilation, Semantic Kernel code gen
│   │   └── ServiceCollectionExtensions.cs
│   ├── WinformTTC.App/                # WinForms UI (net10.0-windows) — main application
│   │   ├── Configuration/             # EditorOptions
│   │   ├── ViewModels/                # MainViewModel (MVVM)
│   │   ├── Forms/                     # MainForm + Designer
│   │   └── Controls/                  # ScintillaConfigurator
│   ├── WinformTTC.Inspector/          # Console app (net10.0-windows) — assembly UI inspector
│   │   ├── Loading/                   # InspectionLoadContext
│   │   ├── Analysis/                  # AssemblyAnalyzer, FormInspector, data models
│   │   └── Output/                    # IReportWriter, MarkdownReportWriter
│   └── WinformTTC.Client/             # WinForms app (net10.0-windows) — FlaUI remote control
│       ├── Automation/                # AppConnector, AppController, ControlExtensions, WaitHelpers
│       ├── Commands/                  # CommandParser, ICommand, Generate/CompileRun/Stop/FullWorkflow
│       ├── Configuration/             # ClientOptions
│       └── Forms/                     # ClientForm + Designer
└── tests/
    └── WinformTTC.E2E/                # FlaUI E2E tests (xUnit, Serilog, FluentAssertions)
        ├── Infrastructure/            # AppFixture, ControlExtensions, WaitHelpers, FlaUITestBase
        ├── Logging/                   # TestLogger (Serilog)
        ├── Reporting/                 # HtmlReportGenerator, TestRunResult, TestStepResult
        └── Tests/                     # FormLoadTest, FullWorkflowTest
```

---

## Projects

### WinformTTC.Core — Code Generation & Compilation Engine

Class library with zero Windows dependencies. Reusable in CLI/API/tests.

| Package | Purpose |
|---------|---------|
| `Microsoft.SemanticKernel` | LLM orchestration (Kernel, plugins, chat) |
| `Microsoft.SemanticKernel.Connectors.Ollama` | Local model connector (prerelease) |
| `Microsoft.CodeAnalysis.CSharp` | Roslyn C# compilation |
| `Microsoft.Extensions.Options` | Options pattern for configuration |

**Key services:**
- `CodeGenerationService` — Streams code from Ollama via `IAsyncEnumerable<string>`
- `RoslynCompilationService` — Compile + execute in collectible `SandboxAssemblyLoadContext`
- `CodeExtractor` — Strips markdown fences from LLM output

### WinformTTC.App — Main Application

WinForms UI with MVVM pattern. Thin shell over Core services.

| Package | Purpose |
|---------|---------|
| `CommunityToolkit.Mvvm` | Source-generated ObservableProperty, RelayCommand |
| `Scintilla.NET.WinForms` | Code editor with C# syntax highlighting |
| `Microsoft.Extensions.Hosting` | Generic host (DI + config + logging) |

**Key components:**
- `MainViewModel` — Properties: `PromptText`, `GeneratedCode`, `OutputText`, `StatusMessage`, `IsProcessing`, `Diagnostics`; Commands: `GenerateCodeCommand`, `CompileAndRunCommand`, `StopCommand`
- `MainForm` — Binds to ViewModel via `PropertyChanged` + `InvokeIfRequired` marshalling
- `ScintillaConfigurator` — C# lexer, syntax highlighting, error line markers

### WinformTTC.Inspector — Assembly UI Inspector

Console app that loads a WinForms DLL/EXE via reflection and produces a markdown report of all UI controls.

- **Reflection-only analysis** — no instantiation (target Form may require DI constructor params)
- `InspectionLoadContext` — Non-collectible, probes target directory for satellite DLLs
- Scans all instance fields where type inherits `Control` or `ToolStripItem`
- Handles `ReflectionTypeLoadException` gracefully (reports warnings, continues with loadable types)

### WinformTTC.Client — FlaUI Remote Control

WinForms app that remote-controls the running WinformTTC.App via FlaUI automation.

| Package | Purpose |
|---------|---------|
| `FlaUI.Core` / `FlaUI.UIA3` | UI Automation framework |
| `Microsoft.Extensions.Configuration.*` | JSON config binding |

**Key components:**
- `AppConnector` — Attach to running process or launch new one
- `AppController` — High-level workflows: `GenerateAndWaitAsync`, `CompileRunAndWaitAsync`, `FullWorkflowAsync`
- `CommandParser` — Regex-based natural language → command mapping
- `ControlExtensions` — Adapted from E2E: `FindToolStripButton`, `SetPromptText`, `GetScintillaText` (clipboard workaround for native Scintilla)

**Command syntax:**

| Input | Command |
|-------|---------|
| `stop` / `cancel` | Stop current operation |
| `compile & run` / `compile and run` | Compile & Run only |
| `generate only <prompt>` | Generate code without running |
| `generate <prompt>` | Full workflow (generate + compile + run) |
| Any other text | Full workflow (treat as prompt) |

### WinformTTC.E2E — End-to-End Tests

Black-box FlaUI tests running against the built App executable.

| Package | Purpose |
|---------|---------|
| `FlaUI.Core` / `FlaUI.UIA3` | UI Automation |
| `xUnit` / `Microsoft.NET.Test.Sdk` | Test framework |
| `Serilog` | Structured logging (console + file) |
| `FluentAssertions` | Fluent test assertions |

**Tests:** `FormLoadTest` (5 smoke tests), `FullWorkflowTest` (end-to-end prompt → generate → compile → run)

---

## UI Layout — WinformTTC.App

```
+------------------------------------------------------------------+
| [ToolStrip]  [Generate] [Compile Run] [Stop]   Model: [________] |
+------------------------------------------------------------------+
| Natural Language Input (TextBox, Multiline, ~3 lines)             |
+==================================================================+
|                                                                   |
|  Generated Code (Scintilla.NET)                                   |
|  - C# syntax highlighting, line numbers, editable                 |
|                                                                   |
+==================================================================+
| Output / Results (RichTextBox, ReadOnly)                          |
+------------------------------------------------------------------+
| [StatusStrip]  Ready  |  ████░░  |  Model: qwen2.5-coder:7b     |
+------------------------------------------------------------------+
```

**AccessibleName mappings** (used by E2E tests and Client):
`"Prompt Input"`, `"Code Editor"`, `"Output Display"`, `"Generate"`, `"Compile & Run"`, `"Stop"`

## UI Layout — WinformTTC.Client

```
+------------------------------------------------------------------+
| [Attach]  [Detach]                     Connection: Not Connected  |
+------------------------------------------------------------------+
| Command Input (TextBox, Multiline)                                |
| Placeholder: e.g. generate the code `1+1=?`                      |
+------------------------------------------------------------------+
| [Send]  [Clear]                                                   |
+------------------------------------------------------------------+
|                                                                   |
| Activity Log (RichTextBox, ReadOnly, Consolas 9pt)                |
| [21:30:00] Attached to WinformTTC.App (PID 1234)                 |
| [21:30:05] Parsed command: Full workflow for "1+1=?"              |
| [21:30:06] Setting prompt text...                                 |
| [21:30:07] Clicking Generate...                                   |
| [21:30:20] Generation completed. Code: 125 chars                  |
| [21:30:21] Clicking Compile & Run...                              |
| [21:30:25] Execution completed. Output: 42 chars                  |
|                                                                   |
+------------------------------------------------------------------+
| StatusStrip: Connected | Target: WinformTTC.App (PID 1234)       |
+------------------------------------------------------------------+
```

---

## Configuration

### appsettings.json (App)

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

### appsettings.client.json (Client)

```json
{
  "Client": {
    "AppPath": "",
    "WindowLoadTimeoutSeconds": 30,
    "GenerationTimeoutSeconds": 120,
    "CompilationTimeoutSeconds": 30,
    "DefaultPollIntervalMs": 250
  }
}
```

### appsettings.e2e.json (E2E Tests)

```json
{
  "AppPath": "",
  "Timeouts": {
    "WindowLoadSeconds": 30,
    "GenerationCompleteSeconds": 120,
    "ExecutionCompleteSeconds": 30
  }
}
```

---

## Key Design Decisions

### Compilation Sandbox
`SandboxAssemblyLoadContext` (collectible, `isCollectible: true`) loads user-generated assemblies. After execution, context is unloaded and GC'd to prevent memory leaks.

### Scintilla Text Access
Scintilla.NET is a native Win32 control that doesn't expose text via UIA patterns. Both E2E tests and Client use a **clipboard workaround**: Focus → Ctrl+A → Ctrl+C → read clipboard → restore original clipboard.

### FlaUI Button Clicks
`InvokePattern.Invoke()` is synchronous and blocks until the button handler completes — this causes UIA COM timeouts (~20s) for long-running async handlers like code generation. Instead, we bring the target window to the foreground via `Window.Focus()` and use physical `Mouse.Click()` which is fire-and-forget.

### UIA Resilience
When the target App is temporarily unresponsive (e.g., during heavy streaming), UIA COM calls can block and throw `COMException`. The Client's `WaitHelpers` catches these transiently and continues polling until the application-level timeout (120s) expires.

### FlaUI + WinForms Namespace Clash
Both `FlaUI.Core.Application` and `System.Windows.Forms.Application` exist in the Client project. Always fully qualify `FlaUI.Core.Application` to avoid `CS0104` ambiguity errors.

---

## Quick Start

### Prerequisites
- .NET 10.0 SDK (10.0.102+)
- [Ollama](https://ollama.com/) running locally with a code model pulled

```bash
ollama pull qwen2.5-coder:7b-instruct-q5_K_M
ollama serve
```

### Build & Run

```bash
# Build entire solution
dotnet build WinformTTC.sln

# Run the main app
dotnet run --project src/WinformTTC.App

# Run the inspector against the app
dotnet run --project src/WinformTTC.Inspector -- \
  src/WinformTTC.App/bin/Debug/net10.0-windows/WinformTTC.App.dll

# Run the remote-control client (App must be running)
dotnet run --project src/WinformTTC.Client

# Run E2E tests (builds and launches App automatically)
dotnet test tests/WinformTTC.E2E
```

### Inspector Usage

```bash
# Print markdown report to stdout
dotnet run --project src/WinformTTC.Inspector -- <path-to-dll>

# Write report to file
dotnet run --project src/WinformTTC.Inspector -- <path-to-dll> --output report.md
```

### Client Usage

1. Launch WinformTTC.App (or let Client launch it)
2. Launch WinformTTC.Client
3. Click **Attach** — verify "Connected" status
4. Type a prompt (e.g., `產生 c# print hello-world 程式碼`) and click **Send**
5. Activity log shows the full workflow: prompt → generate → compile → run → output

---

## Error Handling

| Scenario | Handling |
|----------|----------|
| Ollama not running | `HttpRequestException` → "Cannot connect to Ollama" |
| Model not found | Surface Ollama error → suggest `ollama list` |
| Generation timeout | `CancellationTokenSource.CancelAfter(120s)` |
| Compilation errors | `EmitResult.Diagnostics` → error indicators in Scintilla + output panel |
| Runtime exception | `TargetInvocationException.InnerException` → message + stack trace |
| Execution timeout | `Task.WhenAny` with 30s delay → "Execution timed out" |
| User cancellation | `OperationCanceledException` → StatusMessage = "Cancelled" |
| Assembly inspection failure | `ReflectionTypeLoadException` → partial report + warnings |
| Client UIA COM timeout | Catch `COMException`, continue polling until app-level timeout |
