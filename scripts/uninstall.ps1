#Requires -RunAsAdministrator
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$InstallDir = "$env:ProgramFiles\sudo-mcp"
$ConfigDir = "$env:LOCALAPPDATA\sudo-mcp"

Write-Host "=== sudo-mcp Uninstallation Script ==="
Write-Host ""

# Remove binary
if (Test-Path "$InstallDir\sudo-mcp.exe") {
    Write-Host "Removing binary: $InstallDir\sudo-mcp.exe"
    Remove-Item "$InstallDir\sudo-mcp.exe" -Force
    Write-Host "Binary removed"

    # Remove install directory if empty
    if ((Get-ChildItem $InstallDir -Force | Measure-Object).Count -eq 0) {
        Remove-Item $InstallDir -Force
    }
} else {
    Write-Host "Binary not found (already removed?): $InstallDir\sudo-mcp.exe"
}

# Remove from PATH
$machinePath = [Environment]::GetEnvironmentVariable("Path", "Machine")
if ($machinePath -like "*$InstallDir*") {
    $newPath = ($machinePath -split ";" | Where-Object { $_ -ne $InstallDir }) -join ";"
    [Environment]::SetEnvironmentVariable("Path", $newPath, "Machine")
    Write-Host "Removed $InstallDir from system PATH"
}

# Ask about logs/config
if (Test-Path $ConfigDir) {
    Write-Host ""
    $response = Read-Host "Remove data directory $ConfigDir (contains audit logs)? (y/N)"
    if ($response -eq "y" -or $response -eq "Y") {
        Remove-Item $ConfigDir -Recurse -Force
        Write-Host "Data directory removed"
    } else {
        Write-Host "Data directory kept"
    }
}

Write-Host ""
Write-Host "=== Uninstallation Complete ==="
