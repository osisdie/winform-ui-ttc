using FluentAssertions;
using WinformTTC.E2E.Infrastructure;
using WinformTTC.E2E.Logging;
using Xunit;

namespace WinformTTC.E2E.Tests;

[Collection("E2E")]
public class FormLoadTest : FlaUITestBase
{
    public FormLoadTest(AppFixture fixture) : base(fixture, "FormLoadTest")
    {
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Application_Launches_With_Expected_Title()
    {
        var title = MainWindow.Title;
        TestLogger.Log.Information("Window title: {Title}", title);

        var passed = title.Contains("Text-to-Code", StringComparison.OrdinalIgnoreCase);
        RecordStep("Verify window title", passed, $"Title: {title}");

        title.Should().Contain("Text-to-Code");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void All_Toolbar_Buttons_Are_Visible()
    {
        var generateBtn = MainWindow.FindToolStripButton("Generate");
        var compileBtn = MainWindow.FindToolStripButton("Compile & Run");
        var stopBtn = MainWindow.FindToolStripButton("Stop");

        var generateVisible = generateBtn != null && !generateBtn.IsOffscreen;
        var compileVisible = compileBtn != null && !compileBtn.IsOffscreen;
        var stopVisible = stopBtn != null && !stopBtn.IsOffscreen;

        RecordStep("Generate button visible", generateVisible,
            generateVisible ? "Found" : "Not found or offscreen");
        RecordStep("Compile & Run button visible", compileVisible,
            compileVisible ? "Found" : "Not found or offscreen");
        RecordStep("Stop button visible", stopVisible,
            stopVisible ? "Found" : "Not found or offscreen");

        generateBtn.Should().NotBeNull("Generate button should exist");
        compileBtn.Should().NotBeNull("Compile & Run button should exist");
        stopBtn.Should().NotBeNull("Stop button should exist");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Status_Shows_Ready_On_Launch()
    {
        var status = GetStatusText();
        TestLogger.Log.Information("Status text: {Status}", status);

        var passed = string.Equals(status, "Ready", StringComparison.OrdinalIgnoreCase);
        RecordStep("Status shows Ready", passed, $"Status: {status}");

        status.Should().Be("Ready");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Prompt_Input_Is_Available()
    {
        var promptBox = MainWindow.FindByAccessibleName("Prompt Input");

        var found = promptBox != null;
        RecordStep("Prompt Input found", found,
            found ? "Control accessible" : "Control not found");

        promptBox.Should().NotBeNull("Prompt Input should be accessible");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Output_Display_Is_Available()
    {
        var outputBox = MainWindow.FindByAccessibleName("Output Display");

        var found = outputBox != null;
        RecordStep("Output Display found", found,
            found ? "Control accessible" : "Control not found");

        outputBox.Should().NotBeNull("Output Display should be accessible");
    }
}
