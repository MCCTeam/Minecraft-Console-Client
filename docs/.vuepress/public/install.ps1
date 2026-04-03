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

# Download with a built-in ASCII progress bar (no external tools required).
# HttpWebRequest streams the body on the main thread so we can update the
# progress bar inline without any Runspace or thread-safety concerns.
$outPath  = Join-Path (Get-Location).Path $OUTPUT
$request  = [System.Net.HttpWebRequest]::Create($downloadUrl)
$response = $request.GetResponse()
$totalBytes = $response.ContentLength

$responseStream = $response.GetResponseStream()
$fileStream     = [System.IO.File]::Create($outPath)
$buffer    = New-Object byte[] 32768
$totalRead = 0

try {
    while ($true) {
        $read = $responseStream.Read($buffer, 0, $buffer.Length)
        if ($read -le 0) { break }
        $fileStream.Write($buffer, 0, $read)
        $totalRead += $read
        if ($totalBytes -gt 0) {
            $pct    = [int]($totalRead * 100 / $totalBytes)
            $filled = '=' * [int]($pct / 2)
            $bar    = $filled.PadRight(50)
            $recv   = [math]::Round($totalRead  / 1MB, 1)
            $total  = [math]::Round($totalBytes / 1MB, 1)
            # Use [Console]::Write with an explicit \r so the cursor returns to
            # column 0 and overwrites the previous bar. Write-Host -NoNewline
            # does not reliably reposition the cursor when the script is run
            # via iex (pipe mode), producing multiple bars on one line.
            $line = "`r[{0}] {1,3}%  {2,6:N1} / {3,6:N1} MB" -f $bar, $pct, $recv, $total
            [Console]::Write($line)
        }
    }
} finally {
    $fileStream.Close()
    $responseStream.Close()
    $response.Close()
}

[Console]::WriteLine()   # end the progress line

Write-Host ""
Write-Host "Downloaded: .\$OUTPUT"
Write-Host "Run with:   .\$OUTPUT --help"
