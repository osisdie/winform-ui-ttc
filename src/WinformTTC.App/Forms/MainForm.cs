using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using WinformTTC.App.Configuration;
using WinformTTC.App.Controls;
using WinformTTC.App.ViewModels;

namespace WinformTTC.App.Forms;

public sealed partial class MainForm : Form
{
    private readonly MainViewModel _viewModel;
    private readonly EditorOptions _editorOptions;
    private bool _suppressPromptChange;
    private bool _suppressCodeChange;

    public MainForm(MainViewModel viewModel, IOptions<EditorOptions> editorOptions)
    {
        _viewModel = viewModel;
        _editorOptions = editorOptions.Value;

        InitializeComponent();
        ScintillaConfigurator.ApplyCSharpConfiguration(codeEditor, _editorOptions);
        WireEvents();
        BindViewModel();
    }

    private void WireEvents()
    {
        generateButton.Click += async (_, _) => await _viewModel.GenerateCodeCommand.ExecuteAsync(null);
        compileButton.Click += async (_, _) => await _viewModel.CompileAndRunCommand.ExecuteAsync(null);
        stopButton.Click += (_, _) => _viewModel.StopCommand.Execute(null);

        promptTextBox.TextChanged += (_, _) =>
        {
            if (_suppressPromptChange)
            {
                return;
            }

            _viewModel.PromptText = promptTextBox.Text;
        };

        codeEditor.TextChanged += (_, _) =>
        {
            if (_suppressCodeChange)
            {
                return;
            }

            _viewModel.GeneratedCode = codeEditor.Text;
        };

        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    private void BindViewModel()
    {
        promptTextBox.Text = _viewModel.PromptText;
        codeEditor.Text = _viewModel.GeneratedCode;
        outputTextBox.Text = _viewModel.OutputText;
        statusLabel.Text = _viewModel.StatusMessage;
        modelTextBox.Text = _viewModel.ModelId;
        modelStatusLabel.Text = $"Model: {_viewModel.ModelId}";

        UpdateProcessingState();
        UpdateButtons();
        UpdateDiagnostics();
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvokeIfRequired(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.PromptText):
                    _suppressPromptChange = true;
                    promptTextBox.Text = _viewModel.PromptText;
                    _suppressPromptChange = false;
                    break;
                case nameof(MainViewModel.GeneratedCode):
                    _suppressCodeChange = true;
                    codeEditor.Text = _viewModel.GeneratedCode;
                    _suppressCodeChange = false;
                    break;
                case nameof(MainViewModel.OutputText):
                    outputTextBox.Text = _viewModel.OutputText;
                    break;
                case nameof(MainViewModel.StatusMessage):
                    statusLabel.Text = _viewModel.StatusMessage;
                    break;
                case nameof(MainViewModel.IsProcessing):
                    UpdateProcessingState();
                    break;
                case nameof(MainViewModel.Diagnostics):
                    UpdateDiagnostics();
                    break;
                case nameof(MainViewModel.ModelId):
                    modelTextBox.Text = _viewModel.ModelId;
                    modelStatusLabel.Text = $"Model: {_viewModel.ModelId}";
                    break;
            }

            UpdateButtons();
        });
    }

    private void UpdateProcessingState()
    {
        progressBar.Visible = _viewModel.IsProcessing;
        progressBar.Style = _viewModel.IsProcessing ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
    }

    private void UpdateButtons()
    {
        generateButton.Enabled = _viewModel.GenerateCodeCommand.CanExecute(null);
        compileButton.Enabled = _viewModel.CompileAndRunCommand.CanExecute(null);
        stopButton.Enabled = _viewModel.StopCommand.CanExecute(null);
    }

    private void UpdateDiagnostics()
    {
        if (string.IsNullOrWhiteSpace(_viewModel.Diagnostics))
        {
            ScintillaConfigurator.ClearErrorMarkers(codeEditor);
            return;
        }

        var lines = ExtractLineNumbers(_viewModel.Diagnostics);
        ScintillaConfigurator.SetErrorMarkers(codeEditor, lines);
    }

    private static IEnumerable<int> ExtractLineNumbers(string diagnostics)
    {
        var matches = Regex.Matches(diagnostics, @"\((\d+),\d+\)");
        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out var line))
            {
                yield return Math.Max(0, line - 1);
            }
        }
    }

    private void InvokeIfRequired(Action action)
    {
        if (InvokeRequired)
        {
            BeginInvoke(action);
            return;
        }

        action();
    }
}
