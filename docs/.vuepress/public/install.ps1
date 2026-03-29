# Minecraft Console Client - Installer for Windows
# Downloads the latest MinecraftClient binary for your Windows architecture.
# Usage (PowerShell): iwr -useb https://mccteam.github.io/install.ps1 | iex

$ErrorActionPreference = 'Stop'

$REPO   = "MCCTeam/Minecraft-Console-Client"
$OUTPUT = "MinecraftClient.exe"

# --- Detect CPU architecture ---
$arch = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture
$archId = switch ($arch) {
    'X64'   { 'x64'   }
    'X86'   { 'x86'   }
    'Arm64' { 'arm64' }
    default {
        Write-Error "Unsupported CPU architecture: $arch"
        exit 1
    }
}

$suffix = "win-$archId"

# --- Fetch latest release metadata from GitHub API ---
$apiUrl = "https://api.github.com/repos/$REPO/releases/latest"
Write-Host "Fetching latest release information..."
$release = Invoke-RestMethod -Uri $apiUrl -UseBasicParsing

# --- Locate the correct asset ---
$asset = $release.assets | Where-Object { $_.name -match "^MinecraftClient-.*-$([regex]::Escape($suffix))\.exe$" } | Select-Object -First 1

if (-not $asset) {
    Write-Error "Could not find a release asset for '$suffix'."
    exit 1
}

$downloadUrl = $asset.browser_download_url
$tag         = $release.tag_name

Write-Host "Downloading MinecraftClient $tag ($suffix)..."

# Suppress the progress bar to avoid cluttering the terminal and speed up the download
$ProgressPreference = 'SilentlyContinue'
Invoke-WebRequest -Uri $downloadUrl -OutFile $OUTPUT -UseBasicParsing

Write-Host ""
Write-Host "Downloaded: .\$OUTPUT"
Write-Host "Run with:   .\$OUTPUT --help"
