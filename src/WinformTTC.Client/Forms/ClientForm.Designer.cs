namespace WinformTTC.Client.Forms;

partial class ClientForm
{
    private System.ComponentModel.IContainer components = null!;

    private Button attachButton = null!;
    private Button detachButton = null!;
    private Label connectionLabel = null!;
    private TextBox commandTextBox = null!;
    private Button sendButton = null!;
    private Button clearButton = null!;
    private RichTextBox activityLog = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel connectionStatusLabel = null!;
    private ToolStripStatusLabel targetStatusLabel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        var toolbarPanel = new Panel();
        var commandPanel = new Panel();
        var buttonPanel = new Panel();

        attachButton = new Button();
        detachButton = new Button();
        connectionLabel = new Label();
        commandTextBox = new TextBox();
        sendButton = new Button();
        clearButton = new Button();
        activityLog = new RichTextBox();
        statusStrip = new StatusStrip();
        connectionStatusLabel = new ToolStripStatusLabel();
        targetStatusLabel = new ToolStripStatusLabel();

        this.SuspendLayout();
        toolbarPanel.SuspendLayout();
        commandPanel.SuspendLayout();
        buttonPanel.SuspendLayout();
        statusStrip.SuspendLayout();

        // toolbarPanel
        toolbarPanel.Dock = DockStyle.Top;
        toolbarPanel.Height = 40;
        toolbarPanel.Padding = new Padding(5);
        toolbarPanel.Controls.Add(connectionLabel);
        toolbarPanel.Controls.Add(detachButton);
        toolbarPanel.Controls.Add(attachButton);

        // attachButton
        attachButton.Text = "Attach";
        attachButton.Dock = DockStyle.Left;
        attachButton.Width = 80;

        // detachButton
        detachButton.Text = "Detach";
        detachButton.Dock = DockStyle.Left;
        detachButton.Width = 80;
        detachButton.Enabled = false;

        // connectionLabel
        connectionLabel.Text = "Not Connected";
        connectionLabel.Dock = DockStyle.Fill;
        connectionLabel.TextAlign = ContentAlignment.MiddleRight;
        connectionLabel.Padding = new Padding(0, 0, 10, 0);

        // commandPanel
        commandPanel.Dock = DockStyle.Top;
        commandPanel.Height = 80;
        commandPanel.Padding = new Padding(5, 0, 5, 0);
        commandPanel.Controls.Add(commandTextBox);

        // commandTextBox
        commandTextBox.Dock = DockStyle.Fill;
        commandTextBox.Multiline = true;
        commandTextBox.ScrollBars = ScrollBars.Vertical;
        commandTextBox.PlaceholderText = "e.g. generate the code `1+1=?`";
        commandTextBox.AccessibleName = "Command Input";

        // buttonPanel
        buttonPanel.Dock = DockStyle.Top;
        buttonPanel.Height = 35;
        buttonPanel.Padding = new Padding(5, 2, 5, 2);
        buttonPanel.Controls.Add(clearButton);
        buttonPanel.Controls.Add(sendButton);

        // sendButton
        sendButton.Text = "Send";
        sendButton.Dock = DockStyle.Left;
        sendButton.Width = 80;
        sendButton.Enabled = false;

        // clearButton
        clearButton.Text = "Clear";
        clearButton.Dock = DockStyle.Left;
        clearButton.Width = 80;

        // activityLog
        activityLog.Dock = DockStyle.Fill;
        activityLog.ReadOnly = true;
        activityLog.BackColor = System.Drawing.SystemColors.Window;
        activityLog.Font = new System.Drawing.Font("Consolas", 9F);
        activityLog.AccessibleName = "Activity Log";

        // statusStrip
        statusStrip.Items.AddRange(new ToolStripItem[] { connectionStatusLabel, targetStatusLabel });

        // connectionStatusLabel
        connectionStatusLabel.Text = "Disconnected";

        // targetStatusLabel
        targetStatusLabel.Text = "";
        targetStatusLabel.Spring = true;
        targetStatusLabel.TextAlign = ContentAlignment.MiddleLeft;

        // ClientForm
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 500);
        this.Controls.Add(activityLog);
        this.Controls.Add(buttonPanel);
        this.Controls.Add(commandPanel);
        this.Controls.Add(toolbarPanel);
        this.Controls.Add(statusStrip);
        this.Name = "ClientForm";
        this.Text = "WinformTTC Client";
        this.StartPosition = FormStartPosition.CenterScreen;

        toolbarPanel.ResumeLayout(false);
        commandPanel.ResumeLayout(false);
        commandPanel.PerformLayout();
        buttonPanel.ResumeLayout(false);
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
