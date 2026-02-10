using ScintillaNet.WinForms;

namespace WinformTTC.App.Forms;

partial class MainForm
{
    private ToolStrip toolStrip = null!;
    private ToolStripButton generateButton = null!;
    private ToolStripButton compileButton = null!;
    private ToolStripButton stopButton = null!;
    private ToolStripLabel modelLabel = null!;
    private ToolStripTextBox modelTextBox = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel statusLabel = null!;
    private ToolStripProgressBar progressBar = null!;
    private ToolStripStatusLabel modelStatusLabel = null!;
    private SplitContainer outerSplit = null!;
    private SplitContainer innerSplit = null!;
    private TextBox promptTextBox = null!;
    private Scintilla codeEditor = null!;
    private RichTextBox outputTextBox = null!;

    private void InitializeComponent()
    {
        toolStrip = new ToolStrip();
        generateButton = new ToolStripButton();
        compileButton = new ToolStripButton();
        stopButton = new ToolStripButton();
        modelLabel = new ToolStripLabel();
        modelTextBox = new ToolStripTextBox();
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();
        progressBar = new ToolStripProgressBar();
        modelStatusLabel = new ToolStripStatusLabel();
        outerSplit = new SplitContainer();
        innerSplit = new SplitContainer();
        promptTextBox = new TextBox();
        codeEditor = new Scintilla();
        outputTextBox = new RichTextBox();

        SuspendLayout();

        toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        toolStrip.Items.AddRange(new ToolStripItem[] { generateButton, compileButton, stopButton, new ToolStripSeparator(), modelLabel, modelTextBox });
        toolStrip.Dock = DockStyle.Top;

        generateButton.Text = "Generate";
        generateButton.AccessibleName = "Generate";
        compileButton.Text = "Compile & Run";
        compileButton.AccessibleName = "Compile & Run";
        stopButton.Text = "Stop";
        stopButton.AccessibleName = "Stop";

        modelLabel.Text = "Model:";
        modelTextBox.ReadOnly = true;
        modelTextBox.BorderStyle = BorderStyle.FixedSingle;
        modelTextBox.Width = 180;

        statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, progressBar, modelStatusLabel });
        statusLabel.Text = "Ready";
        progressBar.Style = ProgressBarStyle.Blocks;
        progressBar.Visible = false;
        progressBar.Width = 120;
        modelStatusLabel.Text = "Model: -";

        outerSplit.Dock = DockStyle.Fill;
        outerSplit.Orientation = Orientation.Horizontal;
        outerSplit.SplitterDistance = 90;

        promptTextBox.Dock = DockStyle.Fill;
        promptTextBox.Multiline = true;
        promptTextBox.AccessibleName = "Prompt Input";
        promptTextBox.ScrollBars = ScrollBars.Vertical;
        promptTextBox.Font = new System.Drawing.Font("Consolas", 10F);

        innerSplit.Dock = DockStyle.Fill;
        innerSplit.Orientation = Orientation.Horizontal;
        innerSplit.SplitterDistance = 320;

        codeEditor.Dock = DockStyle.Fill;
        codeEditor.AccessibleName = "Code Editor";
        outputTextBox.Dock = DockStyle.Fill;
        outputTextBox.AccessibleName = "Output Display";
        outputTextBox.ReadOnly = true;
        outputTextBox.BackColor = System.Drawing.SystemColors.Window;
        outputTextBox.Font = new System.Drawing.Font("Consolas", 10F);

        outerSplit.Panel1.Controls.Add(promptTextBox);
        outerSplit.Panel2.Controls.Add(innerSplit);
        innerSplit.Panel1.Controls.Add(codeEditor);
        innerSplit.Panel2.Controls.Add(outputTextBox);

        Controls.Add(outerSplit);
        Controls.Add(statusStrip);
        Controls.Add(toolStrip);

        Text = "Text-to-Code — WinForms";
        MinimumSize = new System.Drawing.Size(900, 700);

        ResumeLayout(false);
        PerformLayout();
    }
}
