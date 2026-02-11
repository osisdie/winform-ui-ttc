using Microsoft.Extensions.Configuration;
using WinformTTC.Client.Automation;
using WinformTTC.Client.Commands;
using WinformTTC.Client.Configuration;

namespace WinformTTC.Client.Forms;

public partial class ClientForm : Form
{
    private readonly ClientOptions _options;
    private readonly AppConnector _connector;
    private AppController? _controller;

    public ClientForm()
    {
        InitializeComponent();

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.client.json", optional: true)
            .Build();

        _options = new ClientOptions();
        config.GetSection("Client").Bind(_options);

        _connector = new AppConnector();

        attachButton.Click += AttachButton_Click;
        detachButton.Click += DetachButton_Click;
        sendButton.Click += SendButton_Click;
        clearButton.Click += ClearButton_Click;
        commandTextBox.KeyDown += CommandTextBox_KeyDown;

        this.FormClosing += ClientForm_FormClosing;
    }

    private async void AttachButton_Click(object? sender, EventArgs e)
    {
        attachButton.Enabled = false;
        var progress = new Progress<string>(LogMessage);

        try
        {
            await _connector.AttachAsync(_options, progress);
            _controller = new AppController(_connector, _options);
            UpdateConnectionState(true);
        }
        catch (Exception ex)
        {
            LogMessage($"Failed to attach: {ex.Message}");
            UpdateConnectionState(false);
        }
    }

    private void DetachButton_Click(object? sender, EventArgs e)
    {
        var progress = new Progress<string>(LogMessage);
        _connector.Detach(progress);
        _controller = null;
        UpdateConnectionState(false);
    }

    private async void SendButton_Click(object? sender, EventArgs e)
    {
        var input = commandTextBox.Text.Trim();
        if (string.IsNullOrEmpty(input) || _controller == null)
            return;

        sendButton.Enabled = false;
        var progress = new Progress<string>(LogMessage);

        try
        {
            var command = CommandParser.Parse(input);
            LogMessage($"Parsed command: {command.Description}");

            var result = await command.ExecuteAsync(_controller, progress);
            LogMessage(result.Success
                ? $"Success: {result.Message}"
                : $"Failed: {result.Message}");
        }
        catch (Exception ex)
        {
            LogMessage($"Error: {ex.Message}");
        }
        finally
        {
            sendButton.Enabled = _connector.IsConnected;
        }
    }

    private void ClearButton_Click(object? sender, EventArgs e)
    {
        activityLog.Clear();
    }

    private void CommandTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && e.Control && sendButton.Enabled)
        {
            e.SuppressKeyPress = true;
            SendButton_Click(sender, e);
        }
    }

    private void ClientForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _connector.Dispose();
    }

    private void UpdateConnectionState(bool connected)
    {
        attachButton.Enabled = !connected;
        detachButton.Enabled = connected;
        sendButton.Enabled = connected;
        connectionLabel.Text = connected ? "Connected" : "Not Connected";
        connectionStatusLabel.Text = connected ? "Connected" : "Disconnected";
        targetStatusLabel.Text = connected && _connector.ProcessId.HasValue
            ? $"Target: WinformTTC.App (PID {_connector.ProcessId})"
            : "";
    }

    private void LogMessage(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => LogMessage(message));
            return;
        }

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        activityLog.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
        activityLog.ScrollToCaret();
    }
}
