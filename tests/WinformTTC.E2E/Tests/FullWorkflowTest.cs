using FluentAssertions;
using FlaUI.Core.Input;
using Microsoft.Extensions.Configuration;
using WinformTTC.E2E.Infrastructure;
using WinformTTC.E2E.Logging;
using Xunit;

namespace WinformTTC.E2E.Tests;

[Collection("E2E")]
public class FullWorkflowTest : FlaUITestBase
{
    public FullWorkflowTest(AppFixture fixture) : base(fixture, "FullWorkflowTest")
    {
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Prompt_Generate_Compile_Run_Full_Workflow()
    {
        // --- Step 1: Enter prompt ---
        var prompt = "Write a C# console application that prints 'Hello, World!' to the console.";
        MainWindow.SetPromptText(prompt);
        Wait.UntilInputIsProcessed();

        RecordStep("Enter prompt", true, $"Prompt: {prompt}");

        // --- Step 2: Click Generate ---
        var generateBtn = MainWindow.FindToolStripButton("Generate");
        generateBtn.Should().NotBeNull("Generate button should exist");
        generateBtn!.ClickToolStripButton();
        TestLogger.Log.Information("Clicked Generate button");
        RecordStep("Click Generate", true, "Generate button clicked");

        // --- Step 3: Wait for generation to start ---
        var generatingTimeout = TimeSpan.FromSeconds(
            Config.GetValue("Timeouts:GeneratingSeconds", 5));

        var generatingStarted = await WaitHelpers.WaitForStatusAsync(
            () => GetStatusText(),
            "Generating code...",
            generatingTimeout);

        RecordStep("Generation started", generatingStarted,
            $"Status: {GetStatusText()}");

        // --- Step 4: Wait for generation to complete ---
        var generationTimeout = TimeSpan.FromSeconds(
            Config.GetValue("Timeouts:GenerationCompleteSeconds", 120));

        var generationCompleted = await WaitHelpers.WaitUntilAsync(
            () =>
            {
                var status = GetStatusText();
                return status == "Code generation completed." ||
                       status == "Code generation failed." ||
                       status == "Cancelled";
            },
            generationTimeout);

        var finalGenStatus = GetStatusText();
        var genPassed = finalGenStatus == "Code generation completed.";
        RecordStep("Generation completed", genPassed,
            $"Status: {finalGenStatus}");

        finalGenStatus.Should().Be("Code generation completed.",
            "Code generation should complete successfully");

        // --- Step 5: Verify code appeared in editor ---
        var codeEditor = MainWindow.FindByAccessibleName("Code Editor");
        codeEditor.Should().NotBeNull("Code Editor should exist");

        var generatedCode = codeEditor!.GetScintillaText();
        var hasCode = !string.IsNullOrWhiteSpace(generatedCode);
        RecordStep("Code generated in editor", hasCode,
            hasCode ? $"Code length: {generatedCode.Length} chars" : "Editor is empty");

        generatedCode.Should().NotBeNullOrWhiteSpace("Generated code should not be empty");

        // --- Step 6: Click Compile & Run ---
        var compileBtn = MainWindow.FindToolStripButton("Compile & Run");
        compileBtn.Should().NotBeNull("Compile & Run button should exist");
        compileBtn!.ClickToolStripButton();
        TestLogger.Log.Information("Clicked Compile & Run button");
        RecordStep("Click Compile & Run", true, "Compile & Run button clicked");

        // --- Step 7: Wait for compilation ---
        var compileTimeout = TimeSpan.FromSeconds(
            Config.GetValue("Timeouts:CompileSeconds", 10));

        var compilingStarted = await WaitHelpers.WaitForStatusContainsAsync(
            () => GetStatusText(),
            "Compil",
            compileTimeout);

        RecordStep("Compilation started", compilingStarted,
            $"Status: {GetStatusText()}");

        // --- Step 8: Wait for execution to complete ---
        var executionTimeout = TimeSpan.FromSeconds(
            Config.GetValue("Timeouts:ExecutionCompleteSeconds", 30));

        var executionCompleted = await WaitHelpers.WaitUntilAsync(
            () =>
            {
                var status = GetStatusText();
                return status == "Execution completed." ||
                       status == "Compilation failed." ||
                       status == "Execution failed." ||
                       status == "Cancelled";
            },
            executionTimeout);

        var finalExecStatus = GetStatusText();
        var execPassed = finalExecStatus == "Execution completed.";
        RecordStep("Execution completed", execPassed,
            $"Status: {finalExecStatus}");

        finalExecStatus.Should().Be("Execution completed.",
            "Execution should complete successfully");

        // --- Step 9: Verify output ---
        var outputBox = MainWindow.FindByAccessibleName("Output Display");
        outputBox.Should().NotBeNull("Output Display should exist");

        var outputText = outputBox!.GetTextBoxText();
        var hasOutput = !string.IsNullOrWhiteSpace(outputText);
        RecordStep("Output produced", hasOutput,
            hasOutput ? $"Output: {outputText.Trim()}" : "Output is empty");

        outputText.Should().NotBeNullOrWhiteSpace("Output should not be empty");

        // --- Step 10: Verify Hello World in output ---
        var containsHello = outputText.Contains("Hello", StringComparison.OrdinalIgnoreCase);
        RecordStep("Output contains expected text", containsHello,
            $"Output: {outputText.Trim()}");

        outputText.Should().Contain("Hello", Exactly.Once(),
            "Output should contain 'Hello'");
    }
}
