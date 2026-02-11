# Plan: WinformTTC.Inspector + WinformTTC.Client

## Context

The WinformTTC solution currently has 3 projects: **App** (WinForms UI), **Core** (code-gen + compilation services), and **E2E** (FlaUI-based tests). The user wants two new capabilities:

1. **Inspector** — A console tool that loads a WinForms DLL/EXE via reflection and produces a report of all UI controls (types, names, properties, hierarchy).
2. **Client** — A WinForms app that remote-controls the running WinformTTC.App via FlaUI, accepting natural-language commands like `"generate the code 1+1=?"`.

---

## Feature 1: WinformTTC.Inspector (Console App)

### New Files

```
src/WinformTTC.Inspector/
├── WinformTTC.Inspector.csproj
├── Program.cs                        # CLI entry point, arg parsing
├── Loading/
│   └── InspectionLoadContext.cs       # Custom AssemblyLoadContext, probes target dir
├── Analysis/
│   ├── AssemblyAnalyzer.cs            # Orchestrator: load assembly, find Forms/Controls
│   ├── FormInspector.cs               # Inspect one Form type: fields, hierarchy, events
│   ├── ControlInfo.cs                 # Record: field name, type, AccessibleName, properties, children
│   ├── FormReport.cs                  # Record: form type name, title, list of ControlInfo
│   └── AssemblyReport.cs              # Record: assembly metadata + list of FormReport + warnings
└── Output/
    ├── IReportWriter.cs               # Interface: string Write(AssemblyReport)
    └── MarkdownReportWriter.cs        # Renders report as markdown
```

### .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
  </PropertyGroup>
</Project>
```

Must target `net10.0-windows` + `UseWindowsForms` so `typeof(Form)`, `typeof(Control)`, `typeof(ToolStripItem)` etc. are resolvable at compile time for type comparison.

### Key Design Decisions

- **AssemblyLoadContext** (`InspectionLoadContext`): Non-collectible. Override `Load()` to probe the target assembly's directory for satellite DLLs (e.g. Scintilla.NET). Falls back to default context for BCL/WinForms assemblies. Pattern follows `SandboxAssemblyLoadContext` in `src/WinformTTC.Core/Services/RoslynCompilationService.cs`.
- **No instantiation attempt**: MainForm requires DI constructor params (`MainViewModel`, `IOptions<EditorOptions>`), so `Activator.CreateInstance` will fail. We do **reflection-only analysis** on fields and types — no need to call `InitializeComponent`.
- **Field scanning**: Scan all instance fields (public + non-public) where field type inherits `Control`, `ToolStripItem`, or `ToolStripStatusLabel`. This captures the WinForms Designer pattern where controls are private fields.
- **ToolStripItems**: These inherit `ToolStripItem` (NOT `Control`), so we scan for both hierarchies.
- **Handle `ReflectionTypeLoadException`**: Some types may fail to load (missing deps). Catch and report as warnings while continuing with loadable types.

### CLI Usage

```
dotnet run --project src/WinformTTC.Inspector -- <path-to-dll-or-exe> [--output report.md]
```

Default: prints markdown to stdout. `--output` writes to file.

---

## Feature 2: WinformTTC.Client (WinForms Remote Control App)

### New Files

```
src/WinformTTC.Client/
├── WinformTTC.Client.csproj
├── Program.cs                        # STAThread entry point
├── Forms/
│   ├── ClientForm.cs                  # Main form: event handlers, logging
│   └── ClientForm.Designer.cs         # UI layout
├── Automation/
│   ├── AppConnector.cs                # Attach to / launch WinformTTC.App, get Window
│   ├── AppController.cs               # High-level actions: SetPrompt, Generate, CompileRun
│   ├── ControlExtensions.cs           # Adapted from E2E (remove Serilog dependency)
│   └── WaitHelpers.cs                 # Copied from E2E (namespace change only)
├── Commands/
│   ├── ICommand.cs                    # Interface + CommandResult record
│   ├── CommandParser.cs               # Regex-based NL -> ICommand mapping
│   ├── GenerateCommand.cs             # Set prompt + click Generate + wait
│   ├── CompileRunCommand.cs           # Click Compile & Run + wait
│   ├── StopCommand.cs                 # Click Stop
│   └── FullWorkflowCommand.cs         # Generate + CompileRun end-to-end
├── Configuration/
│   └── ClientOptions.cs               # AppPath, timeouts
└── appsettings.client.json            # Configuration
```

### .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FlaUI.Core" Version="5.0.0" />
    <PackageReference Include="FlaUI.UIA3" Version="5.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.2" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.2" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="10.0.2" />
  </ItemGroup>
  <ItemGroup>
    <Content Include="appsettings.client.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

No project reference to App/Core/E2E — this is a **black-box automation client**.

### UI Layout (ClientForm)

```
+------------------------------------------------------------------+
| [Attach]  [Detach]                  Connection: Not Connected     |
+------------------------------------------------------------------+
| Command Input (TextBox, Multiline, ~80px)                        |
| Placeholder: e.g. generate the code `1+1=?`                     |
+------------------------------------------------------------------+
| [Send]  [Clear]                                                  |
+------------------------------------------------------------------+
|                                                                  |
| Activity Log (RichTextBox, ReadOnly, fill remaining space)       |
| [21:30:00] Attached to WinformTTC.App (PID 1234)                |
| [21:30:05] Parsed command: Generate code for "1+1=?"            |
| [21:30:05] Setting prompt text...                                |
| [21:30:06] Clicking Generate...                                  |
| [21:30:20] Generation completed. Code: 125 chars                 |
|                                                                  |
+------------------------------------------------------------------+
| StatusStrip: Connected | Target: WinformTTC.App (PID 1234)      |
+------------------------------------------------------------------+
```

### Key Design Decisions

- **AppConnector**: Modeled after `tests/WinformTTC.E2E/Infrastructure/AppFixture.cs`. Uses `Process.GetProcessesByName("WinformTTC.App")` to find running instances, or launches from configured path. Uses `FlaUI.Core.Application.Attach(process)` or `.Launch(path)`. Polls for main window with `WaitHelpers.WaitUntilAsync`.
- **AppController**: Wraps `ControlExtensions` methods (from E2E) into high-level workflow methods: `GenerateAndWaitAsync(prompt)`, `CompileRunAndWaitAsync()`, `FullWorkflowAsync(prompt)`. Uses `IProgress<string>` for status updates to the activity log.
- **ControlExtensions / WaitHelpers**: Copied from E2E and adapted — remove `TestLogger`/Serilog references, change namespace. ~200 lines total. Not shared as a library to avoid coupling test infra with production code.
- **CommandParser**: Simple regex-based patterns:
  - `generate (?:the )?code\s*[`"](.+?)[`"]` → FullWorkflowCommand (generate + compile + run)
  - `generate\s+(.+)` → GenerateCommand (generate only)
  - `compile.*run` → CompileRunCommand
  - `stop|cancel` → StopCommand
  - Fallback: treat entire input as a prompt for GenerateCommand
- **Detach vs Close**: Detaching does NOT kill the target app. Only if Client launched the process AND user closes Client, optionally ask to close target.

### Reused Code (from E2E)

| E2E Source File | Client Destination | Modifications |
|---|---|---|
| `Infrastructure/ControlExtensions.cs` | `Automation/ControlExtensions.cs` | Remove `TestLogger.Log.*` calls, change namespace |
| `Infrastructure/WaitHelpers.cs` | `Automation/WaitHelpers.cs` | Namespace change only (no dependencies) |
| `Infrastructure/AppFixture.cs` | `Automation/AppConnector.cs` | Adapted: add Attach-to-existing, Detach without kill |

---

## Solution-Level Changes

### WinformTTC.sln
Add both projects under the `src` solution folder (GUID `{827E0CD3-B72D-47B6-A68D-7590B98EB39B}`).

### WinformTTC.slnx
Add both projects to the `/src/` folder element.

### Directory.Build.props
No changes needed — `EnableWindowsTargeting=true` already set.

---

## Implementation Order

| Phase | Step | Description |
|-------|------|-------------|
| 1 | 1 | Create both `.csproj` files, add to `.sln` and `.slnx`, stub `Program.cs` |
| 1 | 2 | Verify `dotnet build WinformTTC.sln` compiles |
| 2 | 3 | Inspector: `InspectionLoadContext.cs` |
| 2 | 4 | Inspector: Data models (`ControlInfo`, `FormReport`, `AssemblyReport`) |
| 2 | 5 | Inspector: `FormInspector.cs` + `AssemblyAnalyzer.cs` |
| 2 | 6 | Inspector: `IReportWriter` + `MarkdownReportWriter` |
| 2 | 7 | Inspector: `Program.cs` CLI wiring |
| 3 | 8 | Client: Copy+adapt `ControlExtensions.cs` + `WaitHelpers.cs` |
| 3 | 9 | Client: `AppConnector.cs` |
| 3 | 10 | Client: `AppController.cs` |
| 3 | 11 | Client: Command framework (`ICommand`, `CommandParser`, all commands) |
| 3 | 12 | Client: `ClientOptions.cs` + `appsettings.client.json` |
| 3 | 13 | Client: `ClientForm.Designer.cs` + `ClientForm.cs` |
| 3 | 14 | Client: `Program.cs` |

---

## Verification

### Inspector
```bash
dotnet build src/WinformTTC.App
dotnet run --project src/WinformTTC.Inspector -- src/WinformTTC.App/bin/Debug/net10.0-windows/WinformTTC.App.dll
```
Expected: Markdown report listing `MainForm` with all controls (generateButton, compileButton, stopButton, promptTextBox, codeEditor, outputTextBox, statusLabel, etc.)

### Client
1. Launch WinformTTC.App manually (or let Client launch it)
2. Launch Client: `dotnet run --project src/WinformTTC.Client`
3. Click "Attach" — verify connection
4. Type: `generate the code 1+1=?` and click Send
5. Verify: Activity log shows prompt set → Generate clicked → waiting → code generated → Compile & Run clicked → execution completed → output displayed

### Existing E2E Tests
```bash
dotnet test tests/WinformTTC.E2E/ --logger "console;verbosity=detailed"
```
Must still pass (6/6) — no existing code modified.
