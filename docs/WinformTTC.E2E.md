# E2E Testing Project for WinformTTC

## Context

WinformTTC is a WinForms Text-To-Code application (Prompt → Generate → Compile → Run). It currently has no tests. We need a black-box E2E test project that launches the app executable, automates the UI via FlaUI, and produces an HTML report with screenshots and logs.

## Key Findings

- **No AccessibleName set on any control** in `MainForm.Designer.cs` — must add for reliable FlaUI automation
- **Scintilla.NET is a native Win32 control** — doesn't expose text via standard UIA patterns; needs clipboard workaround
- **ToolStripButtons** have `.Text` set ("Generate", "Compile & Run", "Stop") — findable by name in UIA
- **Status messages** are exact strings in ViewModel: `"Ready"`, `"Generating code..."`, `"Code generation completed."`, `"Compiling..."`, `"Running..."`, `"Execution completed."`, `"Compilation failed."`, `"Execution failed."`
- **Directory.Build.props** sets `net10.0` — test project must override to `net10.0-windows`

---

## File Plan

### New Files (test project)

```
tests/WinformTTC.E2E/
├── WinformTTC.E2E.csproj
├── appsettings.e2e.json
├── Infrastructure/
│   ├── AppFixture.cs            # xUnit IAsyncLifetime — launch/close app
│   ├── FlaUITestBase.cs         # Base class with screenshot/step recording
│   ├── WaitHelpers.cs           # Retry/poll patterns for async UI waits
│   ├── ScreenshotHelper.cs      # FlaUI Capture + save to disk
│   └── ControlExtensions.cs     # Find ToolStrip items, Scintilla text, StatusBar
├── Logging/
│   └── TestLogger.cs            # Serilog setup: console + rolling file in logs/
├── Reporting/
│   ├── TestStepResult.cs        # Model: step name, pass/fail, screenshot path
│   ├── TestRunResult.cs         # Model: overall run with step list
│   └── HtmlReportGenerator.cs   # Self-contained HTML with Base64 screenshots
└── Tests/
    ├── FormLoadTest.cs          # Smoke: app launches, controls visible, status "Ready"
    └── FullWorkflowTest.cs      # E2E: prompt → generate → compile → run → verify output
```

### Modified Files

| File | Change |
|------|--------|
| `src/WinformTTC.App/Forms/MainForm.Designer.cs` | Add `AccessibleName` on all controls for UIA |
| `WinformTTC.sln` | Add test project under `tests/` solution folder |

---

## NuGet Packages

| Package | Purpose |
|---------|---------|
| FlaUI.Core 5.0.0 | UI automation core |
| FlaUI.UIA3 5.0.0 | UIA3 provider |
| xunit 2.9.3 | Test framework |
| xunit.runner.visualstudio 3.1.0 | Test runner |
| Microsoft.NET.Test.Sdk 17.12.0 | dotnet test support |
| Serilog 4.3.0 | Structured logging |
| Serilog.Sinks.Console 6.1.1 | Console sink |
| Serilog.Sinks.File 7.0.0 | Rolling file sink |
| FluentAssertions 8.0.0 | Readable assertions |

---

## Implementation Steps

### Step 1: Modify `MainForm.Designer.cs` — Add Accessibility

Add `AccessibleName` properties to all controls inside `InitializeComponent()`:
- `promptTextBox.AccessibleName = "Prompt Input"`
- `codeEditor.AccessibleName = "Code Editor"`
- `outputTextBox.AccessibleName = "Output Display"`
- `generateButton.AccessibleName = "Generate"`
- `compileButton.AccessibleName = "Compile & Run"`
- `stopButton.AccessibleName = "Stop"`
- `statusLabel.AccessibleName = "Status"`
- `progressBar.AccessibleName = "Progress"`

### Step 2: Create test project `.csproj`

- Target `net10.0-windows` (overrides Directory.Build.props)
- `UseWindowsForms=true`, `IsPackable=false`, `IsTestProject=true`
- Black-box: no project references to WinformTTC.App/Core

### Step 3: Add test project to solution

- Add `tests` solution folder to `WinformTTC.sln`
- Add `WinformTTC.E2E.csproj` under it

### Step 4: Implement Logging (`TestLogger.cs`)

- Serilog with Console + RollingFile sinks
- Rolling interval: Day, retain 7 files
- Output to `logs/` directory relative to test output

### Step 5: Implement Infrastructure

- **AppFixture**: Launch `WinformTTC.App.exe` via `Application.Launch()`, wait for main window by title `"Text-to-Code"`, dispose on teardown
- **WaitHelpers**: `WaitUntilAsync(condition, timeout, pollInterval)` for async UI operations, `WaitForMainWindowAsync`
- **ControlExtensions**: Find ToolStripButton by name, get TextBox/RichTextBox text via UIA patterns, get Scintilla text via clipboard fallback, get status bar text
- **ScreenshotHelper**: Capture window/screen, save PNG, return file path
- **FlaUITestBase**: Base class with `IClassFixture<AppFixture>`, step recording list, auto-screenshot, report generation in Dispose

### Step 6: Implement Report Models + Generator

- `TestStepResult`: step name, passed bool, detail, screenshot path, timestamp
- `TestRunResult`: test name, steps list, start/end time, AllPassed computed
- `HtmlReportGenerator`: Self-contained HTML with inline CSS, embedded Base64 screenshots, log output section. Single-file output in `reports/`

### Step 7: Implement Tests

- **FormLoadTest**: Verify window title, all buttons visible, status = "Ready"
- **FullWorkflowTest**: Enter prompt → click Generate → wait for "Code generation completed." → verify code in editor → click Compile & Run → wait for "Execution completed." → verify output. Screenshots at every step.

### Step 8: Add config and gitignore entries

- `appsettings.e2e.json` with timeout settings and app path
- Add `screenshots/`, `logs/`, `reports/`, `TestResults/` to `.gitignore`

---

## Verification

Run from Windows (PowerShell) at solution root:
```powershell
dotnet build WinformTTC.sln -c Debug
dotnet test tests/WinformTTC.E2E/ --logger "console;verbosity=detailed"
```

Check outputs:
- `tests/WinformTTC.E2E/bin/Debug/net10.0-windows/logs/` — rolling log files
- `tests/WinformTTC.E2E/bin/Debug/net10.0-windows/screenshots/` — PNGs per step
- `tests/WinformTTC.E2E/bin/Debug/net10.0-windows/reports/` — HTML report with embedded screenshots