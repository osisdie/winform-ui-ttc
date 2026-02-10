param(
    [switch]$UseWinget,
    [string]$ModelTag = "qwen2.5-coder:7b-instruct-q5_K_M",
    [string]$ModelVersion = "qwen2.5-coder-7b-instruct-q5_K_M",
    [string]$ModelsRoot = "C:\writable\models",
    [switch]$SkipModelPull,
    [string]$OllamaExePath
)

$ErrorActionPreference = "Stop"

function Get-OllamaCommand {
    if (-not [string]::IsNullOrWhiteSpace($OllamaExePath) -and (Test-Path $OllamaExePath)) {
        return $OllamaExePath
    }

    $cmd = Get-Command "ollama" -ErrorAction SilentlyContinue
    if ($null -ne $cmd) {
        return $cmd.Path
    }

    $candidatePaths = @(
        "C:\Program Files\Ollama\ollama.exe",
        "$env:LOCALAPPDATA\Programs\Ollama\ollama.exe",
        "$env:LOCALAPPDATA\Programs\Ollama\ollama app.exe"
    )

    foreach ($path in $candidatePaths) {
        if (Test-Path $path) {
            return $path
        }
    }

    return $null
}

function Test-OllamaInstalled {
    return $null -ne (Get-OllamaCommand)
}

$ollamaExe = Get-OllamaCommand
if ($null -ne $ollamaExe) {
    Write-Host "Ollama is already installed."
    Write-Host "Using Ollama CLI: $ollamaExe"
    & $ollamaExe --version | Write-Host
} else {
    if ($UseWinget -and (Get-Command "winget" -ErrorAction SilentlyContinue)) {
        Write-Host "Installing Ollama with winget..."
        winget install --id Ollama.Ollama -e --source winget
    } else {
        $installerUrl = "https://ollama.com/download/OllamaSetup.exe"
        $installerPath = Join-Path $env:TEMP "OllamaSetup.exe"

        Write-Host "Downloading Ollama installer..."
        Invoke-WebRequest -Uri $installerUrl -OutFile $installerPath

        Write-Host "Running installer..."
        Start-Process -FilePath $installerPath -ArgumentList "/S" -Wait -NoNewWindow

        Remove-Item $installerPath -ErrorAction SilentlyContinue
    }
}

$ollamaExe = Get-OllamaCommand
if ($null -ne $ollamaExe) {
    Write-Host "Ollama installed successfully."
    Write-Host "Using Ollama CLI: $ollamaExe"
    & $ollamaExe --version | Write-Host
} else {
    Write-Host "Ollama installation may require manual steps."
    Write-Host "Please open https://ollama.com/download and install manually."
    exit 1
}

if (-not $SkipModelPull) {
    $modelDir = Join-Path $ModelsRoot $ModelVersion
    New-Item -ItemType Directory -Force -Path $modelDir | Out-Null

    Write-Host "Using model cache: $modelDir"
    $env:OLLAMA_MODELS = $modelDir

    Write-Host "Pulling model: $ModelTag"
    & $ollamaExe pull $ModelTag

    $models = & $ollamaExe list
    if ($models -notmatch [Regex]::Escape($ModelTag)) {
        Write-Host "Model not found after pull. If Ollama runs as a background service,"
        Write-Host "ensure it uses OLLAMA_MODELS=$modelDir and re-run this script."
        Write-Host "You can also pass -OllamaExePath to the CLI binary explicitly."
        exit 2
    }
}
