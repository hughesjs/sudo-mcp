#Requires -RunAsAdministrator
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$InstallDir = "$env:ProgramFiles\sudo-mcp"
$LogDir = "$env:LOCALAPPDATA\sudo-mcp"

Write-Host "Installing sudo-mcp..."

# Check Windows sudo is available
$sudoPath = Get-Command sudo -ErrorAction SilentlyContinue
if (-not $sudoPath) {
    Write-Warning "Windows sudo not found. sudo-mcp requires Windows 11 24H2+ with sudo enabled."
    Write-Warning "Enable it in Settings > System > For Developers > Enable sudo."
}

# Check binary exists
$BinaryPath = Join-Path $ScriptDir "sudo-mcp.exe"
if (-not (Test-Path $BinaryPath)) {
    Write-Error "sudo-mcp.exe not found in $ScriptDir"
    exit 1
}

# Create install directory
if (-not (Test-Path $InstallDir)) {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
}

# Copy binary
Copy-Item $BinaryPath "$InstallDir\sudo-mcp.exe" -Force

# Add to PATH if not already present
$machinePath = [Environment]::GetEnvironmentVariable("Path", "Machine")
if ($machinePath -notlike "*$InstallDir*") {
    [Environment]::SetEnvironmentVariable("Path", "$machinePath;$InstallDir", "Machine")
    Write-Host "Added $InstallDir to system PATH (restart terminal to take effect)"
}

# Create log directory
if (-not (Test-Path $LogDir)) {
    New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
}

Write-Host "Installed to $InstallDir\sudo-mcp.exe"
Write-Host "Log directory: $LogDir"
